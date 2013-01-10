using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace TSP
{
    public class TwoOptimizer
    {
        private PointArray nodes;

        public NNClusterSet ClusterSet { get; set; }

        public TwoOptimizer(PointArray nodes)
        {
            this.nodes = nodes;
        }

        public void Optimize(IntegerPermutation solution)
        {
            int jend = nodes.Length - 1;

            TaskLogger.Text = "Running 2-opt heuristic on solution...";

            for (int i = 0; i < jend; i++)
            {
                for (int j = i + 2; j < jend; j++)
                {
                    int a = solution[i];
                    int b = solution[i + 1];
                    int c = solution[j];
                    int d = solution[j + 1];


                    /*   before          after
                     * -- a  c --     -- a - c --
                     *     \/ 
                     *     /\
                     * -- d  b --     -- d - b --
                     */
                    if (nodes.GetDistance(a, c) + nodes.GetDistance(b, d) <
                        nodes.GetDistance(a, b) + nodes.GetDistance(c, d))
                    {
                        solution.ReverseSubsequence(i + 1, j);
                    }
                }

                TaskLogger.Progress = 100.0 * (double)i / jend;
            }
        }


        public void OptimizeDisjoint(IntegerPermutation solution, IntegerPermutation tabuSolution)
        {
            var tabuList = TabuEdgeCollection.CreateFromTour(tabuSolution);

            int jend = nodes.Length - 1;

            TaskLogger.Text = "Running 2-opt heuristic on disjoint solutions...";

            for (int i = 0; i < jend; i++)
            {
                for (int j = i + 2; j < jend; j++)
                {
                    int a = solution[i];
                    int b = solution[i + 1];
                    int c = solution[j];
                    int d = solution[j + 1];

                    if (tabuList.IsTabu(a, c) || tabuList.IsTabu(b, d))
                        continue;

                    /*   before          after
                     * -- a  c --     -- a - c --
                     *     \/ 
                     *     /\
                     * -- d  b --     -- d - b --
                     */
                    if (nodes.GetDistance(a, c) + nodes.GetDistance(b, d) <
                        nodes.GetDistance(a, b) + nodes.GetDistance(c, d))
                    {
                        solution.ReverseSubsequence(i + 1, j);
                    }
                }

                TaskLogger.Progress = 100.0 * i / jend;
            }
        }

        public void CrossOptimize(IntegerPermutation solution, IntegerPermutation betterSolution)
        {
            var tabuList = TabuEdgeCollection.CreateFromTour(betterSolution);
            var currentDistance = nodes.GetDistance(solution);
            var betterDistance = nodes.GetDistance(betterSolution);
            int end = nodes.Length - 1;

            double overallGain = 0, overallCost = 0;

            TaskLogger.Text = "Running cross 2-opt heuristic both solutions...";

            for (int i = 0; i < end; i++)
            {
                for (int j = i + 2; j < end; j++)
                {
                    int a = solution[i];
                    int b = solution[i + 1];
                    int c = solution[j];
                    int d = solution[j + 1];

                    bool acTabu = tabuList.IsTabu(a, c);
                    bool bdTabu = tabuList.IsTabu(b, d);

                    // Only one edge can be swapped between solution, otherwise it's a mess
                    if (acTabu && bdTabu)
                        continue;

                    var acDistance = nodes.GetDistance(a, c);
                    var bdDistance = nodes.GetDistance(b, d);
                    var abDistance = nodes.GetDistance(a, b);
                    var cdDistance = nodes.GetDistance(c, d);

                    bool improvable = (acDistance + bdDistance < abDistance + cdDistance);

                    // If the solution cannot be improved, skip this
                    if (!improvable)
                        continue;

                    // If target edge switches are not prohibited, proceed as usual
                    if (!acTabu && !bdTabu)
                    {
                        solution.ReverseSubsequence(i + 1, j);
                    }
                    else
                    {
                        int startNode, middleNode, endNode;
                        double delta = 0;

                        if (acTabu)
                        {
                            startNode = a;
                            endNode = c;
                            middleNode = this.FindTwoEdgesPathMiddleNode(a, c);
                            delta = this.ComputeCostOfTraversing(betterSolution, a, middleNode, c);
                        } 
                        else // if (bdTabu)
                        {
                            startNode = b;
                            endNode = d;
                            middleNode = this.FindTwoEdgesPathMiddleNode(b, d);
                            delta = this.ComputeCostOfTraversing(betterSolution, b, middleNode, d);
                        }

                        // Increase in distance of the best solution
                        var cost = delta;
                        // Decerase in distance of the second solution
                        var gain = (abDistance + cdDistance) - (acDistance + bdDistance);

                        if ((gain > 0) && (gain > cost))
                        {
                            var newBetterDistance = betterDistance + cost;
                            var newCurrentDistance = currentDistance - gain;

                            // By performing this move, the solution would be worse, so end here
                            if (newBetterDistance > newCurrentDistance)
                                return;

                            solution.ReverseSubsequence(i + 1, j);

                            int destinationIndex = betterSolution.IndexOf(endNode);
                            betterSolution.MoveBefore(middleNode, destinationIndex);

                            betterDistance = newBetterDistance;
                            currentDistance = newCurrentDistance;

                            overallGain += gain;
                            overallCost += cost;

                            TaskLogger.Text = String.Format("Overall gain: {0:N0}, Overall cost: {1:N0}", overallGain, overallCost);
                        }
                    }
                }

                TaskLogger.Progress = 100.0 * i / end;
            }
        }

        private int FindTwoEdgesPathMiddleNode(int start, int end)
        {
            double minDistance = double.PositiveInfinity;
            int middleNode = -1;
            int length = nodes.Length;
            var searchSpace = Enumerable.Range(0, length);

            if (this.ClusterSet != null)
            {
                searchSpace =
                    this.ClusterSet.NearestNeighborsOf(start).Union(
                    this.ClusterSet.NearestNeighborsOf(end));
            }

            foreach (int i in searchSpace)
            {
                if ((i == start) || (i == end))
                    continue;

                double starti = nodes.GetDistance(start, i);

                if (starti > minDistance)
                    continue;

                double iend = nodes.GetDistance(i, end);

                if (iend > minDistance)
                    continue;

                if (starti + iend < minDistance)
                {
                    minDistance = starti + iend;
                    middleNode = i;
                }
            }

            return middleNode;
        }

        // Compute the cost (increment in solution distance) of inserting a new stop between a and c, namely acMiddleNode
        private double ComputeCostOfTraversing(IntegerPermutation betterSolution, int a, int acMiddleNode, int c)
        {
            // Remember that if we use node n as a middle node between a and c,
            // we must skip n while traveling along the remaining route
            int mnIndex = betterSolution.IndexOf(acMiddleNode);
            int mnNext = betterSolution[betterSolution.GetNextIndex(mnIndex)];
            int mnPrev = betterSolution[betterSolution.GetPrevIndex(mnIndex)];

            // Now we have to travel from (a, n, c) to avoid the use of arc (a, c),
            // INCREASING the length of the path by:
            //     d(a, n) + d(n, c) - d(a, c)
            // However, we can skip n while completing the ciruit, since we visited it
            // before, thus DECREASING the length of the path by:
            //     d(prev(n), n) + d(n, next(n)) - d(prev(n), next(n))

            return
                (nodes.GetDistance(a, acMiddleNode) + nodes.GetDistance(acMiddleNode, c) - nodes.GetDistance(a, c)) -
                (nodes.GetDistance(mnPrev, acMiddleNode) + nodes.GetDistance(acMiddleNode, mnNext) - nodes.GetDistance(mnPrev, mnNext));
                            
        }
    }
}
