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
        private IntegerPermutation currentSolution, betterSolution;
        private TabuEdgeCollection tabuList;

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

            this.currentSolution = solution;
            this.betterSolution = betterSolution;
            this.tabuList = tabuList;

            double overallGain = 0, overallCost = 0;

            TaskLogger.Text = "Running cross 2-opt heuristic on both solutions...";

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
                            delta = this.ComputeCostOfTraversing(a, middleNode, c);
                        } 
                        else // if (bdTabu)
                        {
                            startNode = b;
                            endNode = d;
                            middleNode = this.FindTwoEdgesPathMiddleNode(b, d);
                            delta = this.ComputeCostOfTraversing(b, middleNode, d);
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

        // Find the minimum-distance non-prohibited node that can be inserted between start and end
        private int FindTwoEdgesPathMiddleNode(int start, int end)
        {
            double minDistance = double.PositiveInfinity;
            int middleNode = -1;
            int length = nodes.Length;
            var searchSpace = Enumerable.Range(0, length);

            if (this.ClusterSet != null)
            {
                // Concat also the original search space at the end because if all nearest
                // edges in the clusters are tabu, then we have to try the normal approach
                searchSpace =
                    this.ClusterSet.NearestNeighborsOf(start)
                        .Concat(this.ClusterSet.NearestNeighborsOf(end))
                        .Concat(searchSpace);
            }

            foreach (int node in searchSpace)
            {
                if ((node == start) || (node == end))
                    continue;

                double starti = nodes.GetDistance(start, node);

                // Additional edges may have been already used!!
                if ((starti > minDistance) || tabuList.IsTabu(start, node))
                    continue;

                double iend = nodes.GetDistance(node, end);

                // And this as well
                if ((iend > minDistance) || tabuList.IsTabu(node, end))
                    continue;

                // Now, let's assume (start, node) and (node, end) are not prohibited and their
                // distance is good. If we add that node in between them, the former edges
                // (prev, node) and (node, next) would be replaced by (prev, next), which may
                // in turn be prohibited as well. Let's check
                int nodeNext = betterSolution.GetNext(middleNode);
                int nodePrev = betterSolution.GetPrev(middleNode);

                if (tabuList.IsTabu(nodePrev, nodeNext))
                    continue;

                if (starti + iend < minDistance)
                {
                    minDistance = starti + iend;
                    middleNode = node;
                }
            }

            return middleNode;
        }

        // Compute the cost (increment in solution distance) of inserting a new stop between a and c, namely acMiddleNode
        private double ComputeCostOfTraversing(int a, int acMiddleNode, int c)
        {
            // Remember that if we use node n as a middle node between a and c,
            // we must skip n while traveling along the remaining route
            int mnNext = betterSolution.GetNext(acMiddleNode);
            int mnPrev = betterSolution.GetPrev(acMiddleNode);

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
