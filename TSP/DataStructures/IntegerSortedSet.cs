using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TSP
{
    public class IntegerSortedSet : ISplittableSet<int>
    {
        private struct ArrayNode
        {
            public int Prev, Next;
            public bool Removed;
        }

        private ArrayNode[] nodes;
        private int size, itemsCount;
        private int firstNumber;
        private bool empty;

        public int ItemsCount
        {
            get { return itemsCount; }
        }

        private IntegerSortedSet(int size)
        {
            this.nodes = new ArrayNode[size];
            this.size = size;
            this.itemsCount = 0;
            this.empty = true;
        }

        public void Add(int number)
        {
            if (empty)
            {
                firstNumber = number;
                nodes[number].Prev = -1;
                nodes[number].Next = size;
                nodes[number].Removed = false;
                empty = false;
                itemsCount++;
            }
            else
            {
                if (!nodes[number].Removed)
                    return;

                // Find previous
                for (int i = number - 1; i >= 0; i--)
                {
                    if (!nodes[i].Removed)
                    {
                        nodes[i].Next = number;
                        nodes[number].Prev = i;
                        break;
                    }
                }
                // Find next
                for (int j = number + 1; j < size; j++)
                {
                    if (!nodes[j].Removed)
                    {
                        nodes[j].Prev = number;
                        nodes[number].Next = j;
                        break;
                    }
                }

                nodes[number].Removed = false;
                itemsCount++;
            }
        }

        public void Remove(int number)
        {
            if (empty || nodes[number].Removed)
                return;

            int currentPrev = nodes[number].Prev;
            int currentNext = nodes[number].Next;

            if (currentPrev > -1)
                nodes[currentPrev].Next = currentNext;

            if (currentNext < size)
                nodes[currentNext].Prev = currentPrev;

            if (number == firstNumber)
                firstNumber = currentNext;

            nodes[number].Removed = true;
            itemsCount--;
        }

        public bool Contains(int number)
        {
            return !nodes[number].Removed;
        }

        public IEnumerator<int> GetEnumerator()
        {
            return new InternalEnumerator(this);
        }

        public IEnumerable<IEnumerator<int>> GetDisjointEnumerators(int enumeratorsCount)
        {
            int subsetSize = itemsCount / enumeratorsCount + 1;
            int counter = 0;
            int start = 0;

            foreach (int number in this)
            {
                if (counter == 0)
                    start = number;

                counter++;

                if (counter == subsetSize)
                {
                    var enumerator = new InternalRangeEnumerator(this, start, number);
                    yield return enumerator;
                    counter = 0;
                }
            }

            if (counter > 0)
            {
                var enumerator = new InternalRangeEnumerator(this, start, size);
                yield return enumerator;
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public static IntegerSortedSet CreateFullSet(int size)
        {
            var result = new IntegerSortedSet(size);

            for (int i = 0; i < size; i++)
            {
                result.nodes[i].Prev = i - 1;
                result.nodes[i].Next = i + 1;
                result.nodes[i].Removed = false;
            }

            result.empty = false;
            result.firstNumber = 0;
            result.itemsCount = size;

            return result;
        }

        public static IntegerSortedSet CreateEmptySet(int size)
        {
            var result = new IntegerSortedSet(size);

            for (int i = 0; i < size; i++)
            {
                result.nodes[i].Prev = -1;
                result.nodes[i].Next = size;
                result.nodes[i].Removed = true;
            }

            result.empty = true;
            result.firstNumber = -1;
            result.itemsCount = 0;

            return result;
        }


        private class InternalEnumerator : IEnumerator<int>
        {
            protected IntegerSortedSet enclosingSet;
            protected int index, size;

            public InternalEnumerator(IntegerSortedSet enclosingSet)
            {
                this.enclosingSet = enclosingSet;
                this.index = -1;
                this.size = enclosingSet.size;
            }

            public int Current
            {
                get { return index; }
            }

            object System.Collections.IEnumerator.Current
            {
                get { return index; }
            }

            public void Dispose()
            {
                this.Reset();
            }

            public virtual bool MoveNext()
            {
                if (index >= size)
                    return false;

                if (index < 0)
                    index = enclosingSet.firstNumber;
                else
                    index = enclosingSet.nodes[index].Next;

                if (index >= size)
                    return false;

                return true;
            }

            public void Reset()
            {
                index = -1;
            }
        }

        private class InternalRangeEnumerator : InternalEnumerator
        {
            protected int start, end;

            public InternalRangeEnumerator(IntegerSortedSet enclosingSet, int start, int end) : base(enclosingSet)
            {
                this.start = Math.Max(start, enclosingSet.firstNumber);
                this.end = Math.Min(end, enclosingSet.size - 1);
            }

            public override bool MoveNext()
            {
                if (index > end)
                    return false;

                if (index < 0)
                {
                    if (!enclosingSet.nodes[start].Removed)
                        index = start;
                    else
                        for (int i = start + 1; i <= end; i++)
                        {
                            // Find first valid node after start
                            if (!enclosingSet.nodes[i].Removed)
                            {
                                index = i;
                                break;
                            }
                        }
                }
                else
                    index = enclosingSet.nodes[index].Next;

                if (index > end)
                    return false;

                return true;
            }
        }
    }
}
