using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TSP
{
    public class IntegerPermutation : IEnumerable<int>
    {
        protected volatile int[] data;

        public int Length
        {
            get { return data.Length; }
        }

        public IntegerPermutation(int size)
        {
            this.data = new int[size]; 
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
            int length = Math.Abs(end - start) / 2 + 1;

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
