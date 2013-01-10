using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Horatio
{
    public abstract class BinaryHeap<T> where T : IComparable<T>
    {
        protected T[] heap;
        protected int size;

        public bool FixedCapacity { get; set; }

        public bool IsEmpty
        {
            get { return (size == 0); }
        }

        public bool IsFull
        {
            get { return (size >= heap.Length); }
        }

        public BinaryHeap(int capacity)
        {
            this.heap = new T[capacity];
            this.size = 0;
        }

        protected BinaryHeap() { }

        public void Insert(T item)
        {
            if (this.IsFull)
            {
                if (this.FixedCapacity) throw new InvalidOperationException("The heap has exceeded its maximum capacity!");

                Array.Resize(ref heap, heap.Length * 2);
            }

            heap[size] = item;

            int parent = this.GetParent(size);
            int child = size;

            while (parent >= 0 && !this.CompareRank(heap[parent], heap[child]))
            {
                this.Swap(parent, child);
                child = parent;
                parent = this.GetParent(child);
            }

            size++;
        }

        public T ExtractRoot()
        {
            if (size == 0)
                throw new InvalidOperationException("The heap is empty!");

            T result = heap[0];

            heap[0] = heap[size - 1];
            heap[size - 1] = default(T);
            size--;

            this.Heapify(0);

            return result;
        }

        public T PeekRoot()
        {
            if (size == 0)
                throw new InvalidOperationException("The heap is empty!");

            return heap[0];
        }

        private void Heapify(int index)
        {
            int left = 2 * index + 1;
            int right = left + 1;
            int pivot = index;

            if (left < size && 
                this.CompareRank(heap[left], heap[index]))
                pivot = left;

            if (right < size && 
                this.CompareRank(heap[right], heap[pivot]))
                pivot = right;

            if (pivot != index)
            {
                Swap(index, pivot);
                Heapify(pivot);
            }
        }

        private void Swap(int i, int j)
        {
            T temp = heap[i];
            heap[i] = heap[j];
            heap[j] = temp;
        }

        private int GetParent(int index)
        {
            return (int)Math.Floor((index - 1) / 2.0);
        }

        protected void CreateHeap(IEnumerable<T> items)
        {
            int capacity = items.Count() * 2;

            size = 0;
            heap = new T[capacity];
            foreach (T item in items) 
            {
                heap[size++] = item;
            }

            for (int i = size / 2 - 1; i >= 0; i--)
            {
                this.Heapify(i);
            }
        }

        protected abstract bool CompareRank(T item1, T item2);
    }

    public class MaxHeap<T> : BinaryHeap<T> where T : IComparable<T>
    {
        public MaxHeap(int capacity) : base(capacity) { }

        protected MaxHeap() : base() { }

        protected override bool CompareRank(T item1, T item2)
        {
            return (item1.CompareTo(item2) > 0);
        }

        public static MaxHeap<T> Build(IEnumerable<T> items)
        {
            var result = new MaxHeap<T>();

            result.CreateHeap(items);

            return result;
        }
    }

    public class MinHeap<T> : BinaryHeap<T> where T : IComparable<T>
    {
        public MinHeap(int capacity) : base(capacity) { }

        protected MinHeap() : base() { }

        protected override bool CompareRank(T item1, T item2)
        {
            return (item1.CompareTo(item2) < 0);
        }

        public static MinHeap<T> Build(IEnumerable<T> items)
        {
            var result = new MinHeap<T>();

            result.CreateHeap(items);

            return result;
        }
    }
}
