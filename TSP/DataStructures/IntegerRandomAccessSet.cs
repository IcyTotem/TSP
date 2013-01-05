using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TSP
{
    public class IntegerRandomAccessSet : IEnumerable<int>
    {
        private int[] items;
        private int lastUsedIndex;
        private int capacity;

        public int ItemsCount
        {
            get { return lastUsedIndex + 1; }
        }

        public int this[int index]
        {
            get { return items[index]; }
        }

        private IntegerRandomAccessSet(int capacity)
        {
            this.items = new int[capacity];
            this.lastUsedIndex = -1;
            this.capacity = capacity;
        }

        public void Add(int number)
        {
            int nextIndex = lastUsedIndex + 1;

            if (nextIndex >= capacity)
                throw new InvalidOperationException("Set already full!");

            items[nextIndex] = number;
            lastUsedIndex = nextIndex;
        }

        public void RemoveAt(int index)
        {
            if (index > lastUsedIndex)
                throw new InvalidOperationException("Can't remove at unused position.");

            items[index] = items[lastUsedIndex];
            lastUsedIndex--;
        }

        public static IntegerRandomAccessSet CreateFullSet(int size)
        {
            var result = new IntegerRandomAccessSet(size);

            for (int i = 0; i < size; i++)
                result.items[i] = i;

            result.lastUsedIndex = size - 1;

            return result;
        }

        public static IntegerRandomAccessSet CreateEmptySet(int size)
        {
            return new IntegerRandomAccessSet(size);
        }

        public IEnumerator<int> GetEnumerator()
        {
            for (int i = 0; i <= lastUsedIndex; i++)
                yield return items[i];
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
