using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TSP
{
    public class TabuEdgeCollection
    {
        // Contains as index the head node and as value the tail node
        private int[] tabuDirectArcs, tabuReverseArcs;

        private TabuEdgeCollection() { }

        public bool IsTabu(Edge edge)
        {
            return this.IsTabu(edge.Head, edge.Tail);
        }

        public bool IsTabu(int head, int tail)
        {
            return (tabuDirectArcs[head] == tail) || (tabuDirectArcs[tail] == head) || (tabuReverseArcs[head] == tail) || (tabuReverseArcs[tail] == head);
        }

        public static TabuEdgeCollection CreateFromTour(IntegerPermutation otherTour)
        {
            var result = new TabuEdgeCollection();

            result.tabuDirectArcs = new int[otherTour.Length];
            result.tabuReverseArcs = new int[otherTour.Length];

            int upperBound = otherTour.Length - 1;

            for (int i = 0; i < upperBound; i++)
            {
                int head = otherTour[i];
                int tail = otherTour[i + 1];

                result.tabuDirectArcs[head] = tail;
                result.tabuReverseArcs[tail] = head;
            }

            result.tabuDirectArcs[otherTour[upperBound]] = otherTour[0];
            result.tabuReverseArcs[otherTour[0]] = otherTour[upperBound];

            return result;
        }
    }
}
