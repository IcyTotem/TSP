using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TSP
{
    public class NNTourFinder
    {
        private Random random;

        private IntegerSortedSet visitedNodes, unvisitedNodes;
        private PointArray nodes;
        private TabuEdgeCollection tabuEdgeList;
        
        public NNTourFinder(PointArray nodes)
        {
            this.nodes = nodes;
            this.random = new Random();
        }


        public IEnumerable<int> FindRandomTour()
        {
            return this.FindTourStartingFrom(this.GetRandomEdge());
        }

        public IEnumerable<int> FindTourStartingFrom(Edge startEdge)
        {
            this.InitializeVisit();

            this.VisitNode(startEdge.Head);
            yield return startEdge.Head;

            this.VisitNode(startEdge.Tail);
            yield return startEdge.Tail;

            int currentNode = startEdge.Tail;

            TaskLogger.Text = "Computing nearest neighbor tour...";

            while (unvisitedNodes.ItemsCount > 0)
            {
                int nearestUnvisitedNode = this.FindNearestUnvisitedNode(currentNode);

                this.VisitNode(nearestUnvisitedNode);
                yield return nearestUnvisitedNode;

                currentNode = nearestUnvisitedNode;

                TaskLogger.Progress = 100.0 * visitedNodes.ItemsCount / nodes.Length;
            }
        }

        private Edge GetRandomEdge()
        {
            int random1 = random.Next(0, nodes.Length);
            int random2 = random.Next(0, nodes.Length);

            while (random1 == random2)
                random2 = random.Next(0, nodes.Length);

            return new Edge(random1, random2);
        }

        private int FindNearestUnvisitedNode(int current)
        {
            int nearestUnvisitedNode = -1;
            int minDistance = int.MaxValue;

            foreach (int unvisitedNode in unvisitedNodes)
            {
                int dist = Point.SquareDistance(nodes[current], nodes[unvisitedNode]);
                if (dist < minDistance)
                {
                    nearestUnvisitedNode = unvisitedNode;
                    minDistance = dist;
                }
            }

            return nearestUnvisitedNode;
        }

        private void InitializeVisit()
        {
            unvisitedNodes = IntegerSortedSet.CreateFullSet(nodes.Length);
            visitedNodes = IntegerSortedSet.CreateEmptySet(nodes.Length);
        }

        private void VisitNode(int node)
        {
            visitedNodes.Add(node);
            unvisitedNodes.Remove(node);
        }


        public IEnumerable<int> FindRandomDisjointTour(IntegerPermutation otherTour)
        {
            this.InitializeVisit();
            this.tabuEdgeList = TabuEdgeCollection.CreateFromTour(otherTour);

            var randomEdge = this.GetRandomEdge();

            while (tabuEdgeList.IsTabu(randomEdge))
                randomEdge = this.GetRandomEdge();

            this.VisitNode(randomEdge.Head);
            yield return randomEdge.Head;

            this.VisitNode(randomEdge.Tail);
            yield return randomEdge.Tail;

            int currentNode = randomEdge.Tail;

            TaskLogger.Text = "Computing greedy and tabu search to generate a disjoint tour...";

            while (unvisitedNodes.ItemsCount > 0)
            {
                int nearestUnvisitedNode = this.FindNearestUnvisitedNonTabuNode(currentNode);

                this.VisitNode(nearestUnvisitedNode);
                yield return nearestUnvisitedNode;

                currentNode = nearestUnvisitedNode;

                TaskLogger.Progress = 100.0 * visitedNodes.ItemsCount / nodes.Length;
            }
        }

        private int FindNearestUnvisitedNonTabuNode(int current)
        {
            int nearestUnvisitedNode = -1;
            int minDistance = int.MaxValue;

            foreach (int unvisitedNode in unvisitedNodes)
            {
                if (tabuEdgeList.IsTabu(current, unvisitedNode))
                    continue;

                int dist = Point.SquareDistance(nodes[current], nodes[unvisitedNode]);
                if (dist < minDistance)
                {
                    nearestUnvisitedNode = unvisitedNode;
                    minDistance = dist;
                }
            }

            return nearestUnvisitedNode;
        }
    }
}
