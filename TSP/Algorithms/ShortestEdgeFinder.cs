using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace TSP
{
    public class ShortestEdgeFinder
    {
        private struct ThreadInput
        {
            public int start, end;
        }

        private struct ThreadResult
        {
            public int edgeHead, edgeTail;
            public int edgeLength;
        }

        private Thread[] threads;

        private volatile ThreadInput[] threadInputs;
        private volatile ThreadResult[] threadResults;
        private volatile Point[] nodes;

        public ShortestEdgeFinder(int threadCount)
        {
            if (threadCount < 2)
                throw new InvalidOperationException("This object must be used with at least 2 threads.");

            this.threads = new Thread[threadCount];
            this.threadInputs = new ThreadInput[threadCount];
            this.threadResults = new ThreadResult[threadCount];
        }


        public Edge FindInCompleteGraph(Point[] nodes)
        {
            this.nodes = nodes;

            TaskLogger.Text = string.Format("Computing shortest edge in graph with {0} parallel threads...", threads.Length);
            TaskLogger.Progress = 0.0;

            this.ClearThreadData();
            this.SplitDomain();

            for (int i = 0; i < threads.Length; i++)
                threads[i] = new Thread(ScanSubdomain);

            this.ExecuteThreads();

            return this.AggregateThreadResults();
        }


        private void ClearThreadData()
        {
            for (int i = 0; i < threads.Length; i++)
            {
                threadResults[i].edgeHead = 0;
                threadResults[i].edgeTail = 0;
                threadResults[i].edgeLength = 0;
            }
        }

        private void ExecuteThreads()
        {
            for (int i = 0; i < threads.Length; i++)
                threads[i].Start(i);

            for (int i = 0; i < threads.Length; i++)
                threads[i].Join();
        }

        private Edge AggregateThreadResults()
        {
            int absoluteMinDistance = int.MaxValue;
            int endPoint1 = -1, endPoint2 = -1;

            for (int i = 0; i < threads.Length; i++)
            {
                var res = threadResults[i];
                if (res.edgeLength < absoluteMinDistance)
                {
                    endPoint1 = res.edgeHead;
                    endPoint2 = res.edgeTail;
                    absoluteMinDistance = res.edgeLength;
                }
            }

            return new Edge(endPoint1, endPoint2);
        }


        private void SplitDomain()
        {
            int nodesCount = nodes.Length;

            if (nodesCount < threads.Length)
                throw new InvalidOperationException("This component causes too much overhead for the given number of nodes!");

            int subdomainSize = nodesCount / threads.Length;
            int counter = 0;

            for (int i = 0; i < threads.Length - 1; i++)
            {
                threadInputs[i].start = counter;
                threadInputs[i].end = counter + subdomainSize;
                counter += subdomainSize + 1;
            }

            // Allocates the last one summing up the eccess yield by rounding
            threadInputs[threads.Length - 1] = new ThreadInput() { start = counter, end = nodesCount - 1 };
        }

        private void ScanSubdomain(object threadIndex)
        {
            int index = (int)threadIndex;

            int minDist = int.MaxValue;
            int minHead = -1;
            int minTail = -1;

            int start = threadInputs[index].start;
            int end = threadInputs[index].end + 1;

            for (int i = start; i < end; i++)
            {
                for (int j = i + 1; j < nodes.Length; j++)
                {
                    int dist = Point.SquareDistance(nodes[i], nodes[j]);

                    if (dist < minDist)
                    {
                        minHead = i;
                        minTail = j;
                        minDist = dist;
                    }
                }
                TaskLogger.Progress += 100.0 / nodes.Length;
            }

            threadResults[index].edgeHead = minHead;
            threadResults[index].edgeTail = minTail;
            threadResults[index].edgeLength = minDist;
        }
    }
}
