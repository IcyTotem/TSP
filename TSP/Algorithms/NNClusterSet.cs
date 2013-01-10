using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;

namespace TSP
{
    public class NNClusterSet
    {
        private struct DistanceNode : IComparable<DistanceNode>
        {
            public int index;
            public double distance;

            public int CompareTo(DistanceNode other)
            {
                return distance.CompareTo(other.distance);
            }
        }

        private volatile PointArray nodes;
        private volatile int[,] clusters;
        private volatile int clusterSize;

        private NNClusterSet(PointArray nodes, int clusterSize)
        {
            this.clusterSize = clusterSize;
            this.nodes = nodes;
            this.clusters = new int[nodes.Length, clusterSize];
        }

        public IEnumerable<int> NearestNeighborsOf(int nodeIndex)
        {
            for (int i = 0; i < clusterSize; i++)
                yield return clusters[nodeIndex, i];
        }

        private void Initialize(int threadCount)
        {
            var threads = new Thread[threadCount];

            TaskLogger.Text = "Clustering with nearest neighbors...";
            TaskLogger.Progress = 0;

            for (int index = 0; index < threadCount; index++)
                threads[index] = ThreadStarter.Start(ThreadedSearch, index, threadCount);

            threads.JoinAll();
        }

        private void ThreadedSearch(int threadIndex, int threadCount)
        {
            int length = nodes.Length;
            
            // Better safe than sorry
            int subdomainSize = Math.Min(length / threadCount + 1, length);
            int start = threadIndex * subdomainSize;
            int end = Math.Min(start + subdomainSize + 1, length); 

            for (int i = start; i < end; i++)
            {
                // The max heap automatically arranges itself to contain the maximum as root in O(log(n))
                // Since clusterSize is generally small, it is a better choice than an ordered list
                var maxHeap = new Horatio.MaxHeap<DistanceNode>(clusterSize);
                var maxDistance = double.PositiveInfinity;
                int j;

                maxHeap.FixedCapacity = true;

                // Fill the heap
                for (j = 0; !maxHeap.IsFull; j++)
                {
                    if (j == i)
                        continue;
                    maxHeap.Insert(new DistanceNode() { index = j, distance = nodes.GetDistance(i, j) });
                }
                maxDistance = maxHeap.PeekRoot().distance;

                // Proceed with normal search
                for ( ; j < length; j++)
                {
                    var distance = nodes.GetDistance(i, j);

                    if (distance < maxDistance)
                    {
                        maxHeap.ExtractRoot();
                        maxHeap.Insert(new DistanceNode() { index = j, distance = distance });
                        maxDistance = maxHeap.PeekRoot().distance;
                    }
                }

                // Transfer the nearest nodes from the heap to the array
                for (int counter = clusterSize - 1; counter >= 0; counter--)
                    clusters[i, counter] = maxHeap.ExtractRoot().index;

                TaskLogger.Progress += (100.0 / threadCount) * ((double)(i - start) / (end - start));
            }
        }

        public void SerializeOn(Stream stream)
        {
            int length = nodes.Length;

            using (var writer = new BinaryWriter(stream))
            {
                for (int i = 0; i < length; i++)
                {
                    for (int j = 0; j < clusterSize; j++)
                        writer.Write(clusters[i, j]);
                }
            }
        }

        public static NNClusterSet DeserializeFrom(Stream stream, PointArray nodes, int clusterSize)
        {
            var result = new NNClusterSet(nodes, clusterSize);
            int length = nodes.Length;

            using (var reader = new BinaryReader(stream))
            {
                for (int i = 0; i < length; i++)
                {
                    for (int j = 0; j < clusterSize; j++)
                        result.clusters[i, j] = reader.ReadInt32();
                }
            }

            return result;
        }

        public static NNClusterSet Build(PointArray nodes, int clusterSize, int threadsUsedForInitialization = 10)
        {
            var result = new NNClusterSet(nodes, clusterSize);

            result.Initialize(threadsUsedForInitialization);

            return result;
        }
    }
}
