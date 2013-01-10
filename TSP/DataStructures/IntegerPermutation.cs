using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TSP
{
    public class IntegerPermutation : IEnumerable<int>
    {
        private Queue<int> recentIndices;

        protected volatile int[] data;

        public int Length
        {
            get { return data.Length; }
        }

        public IntegerPermutation(int size)
        {
            this.data = new int[size];
            this.recentIndices = new Queue<int>();
        }

        public IntegerPermutation(IEnumerable<int> original)
        {
            if (original.GetType().IsAssignableFrom(typeof(IntegerPermutation)))
            {
                var originalData = ((IntegerPermutation)original).data;
                this.data = new int[originalData.Length];
                Array.Copy(originalData, this.data, originalData.Length);
                return;
            }

            this.data = (new List<int>(original)).ToArray();
            this.recentIndices = new Queue<int>();
        }

        public virtual int this[int index]
        {
            get { return data[index]; }
            set { data[index] = value; }
        }

        public virtual IEnumerable<int> this[int startIndex, int endIndex]
        {
            get { return this.Subsequence(startIndex, endIndex); }
            set
            {
                int i = startIndex;
                foreach (int number in value)
                {
                    data[i++] = number;
                    if (i > endIndex)
                        break;
                }
            }
        }

        public IEnumerable<int> Subsequence(int start, int end)
        {
            start = start % data.Length;
            end = end % data.Length;

            if (start > end)
            {
                int temp = start;
                start = end;
                end = temp;
            }

            for (int i = start; i <= end; i++)
                yield return data[i];
        }

        public void ReverseSubsequence(int start, int end)
        {
            int diff = Math.Abs(end - start);
            int length = (int)Math.Ceiling(diff / 2.0);

            while (length > 0)
            {
                int temp = data[start];
                data[start] = data[end];
                data[end] = temp;

                start = this.GetNextIndex(start);
                end = this.GetPrevIndex(end);

                length--;
            }
        }

        // Extract element from the permutation, insert it before newIndex and shift the remaining elements to fit
        public void MoveBefore(int element, int newIndex)
        {
            int elementIndex = this.IndexOf(element);
            newIndex = this.GetPrevIndex(newIndex);

            if (newIndex < elementIndex)
            {
                for (int i = elementIndex - 1; i >= newIndex; i--)
                    data[i + 1] = data[i];
                data[newIndex] = element;
            }
            else if (newIndex > elementIndex)
            {
                for (int i = elementIndex + 1; i <= newIndex; i++)
                    data[i - 1] = data[i];
                data[newIndex] = element;
            }
        }

        public int GetNextIndex(int index)
        {
            return (index + 1) % data.Length;
        }

        public int GetPrevIndex(int index)
        {
            if (index - 1 >= 0)
                return index - 1;

            return data.Length + index - 1;
        }

        public int GetNext(int element)
        {
            // Search in last 10 indices
            foreach (int recentIndex in recentIndices)
                if (data[recentIndex] == element)
                    return data[this.GetNextIndex(recentIndex)];

            // Otheriwse, proceed normally
            int index = this.IndexOf(element);
            return data[this.GetNextIndex(index)];
        }

        public int GetPrev(int element)
        {
            foreach (int recentIndex in recentIndices)
                if (data[recentIndex] == element)
                    return data[this.GetPrevIndex(recentIndex)];

            int index = this.IndexOf(element);
            return data[this.GetPrevIndex(index)];
        }

        public int IndexOf(int element)
        {
            int index = Array.IndexOf(data, element);

            // Store index for future uses
            if (index > -1)
            {
                recentIndices.Enqueue(index);

                if (recentIndices.Count > 10)
                    recentIndices.Dequeue();
            }

            return index;
        }

        public IEnumerator<int> GetEnumerator()
        {
            for (int i = 0; i < data.Length; i++)
                yield return data[i];
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
