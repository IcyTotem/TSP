using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace TSP
{
    public class TwoOptimizer
    {
        private Point[] nodes;

        public TwoOptimizer(Point[] nodes)
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
                    if (Point.Distance(nodes[a], nodes[c]) + Point.Distance(nodes[b], nodes[d]) <
                        Point.Distance(nodes[a], nodes[b]) + Point.Distance(nodes[c], nodes[d]))
                    {
                        //solution[i + 1, j] = solution[i + 1, j].Reverse();
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

            TaskLogger.Text = "Running 2-opt heuristic and tabu search on disjoint solutions...";

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
                    if (Point.Distance(nodes[a], nodes[c]) + Point.Distance(nodes[b], nodes[d]) <
                        Point.Distance(nodes[a], nodes[b]) + Point.Distance(nodes[c], nodes[d]))
                    {
                        //solution[i + 1, j] = solution[i + 1, j].Reverse();
                        solution.ReverseSubsequence(i + 1, j);
                    }
                }

                TaskLogger.Progress = 100.0 * i / jend;
            }
        }
    }
}
