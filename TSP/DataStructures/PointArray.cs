using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TSP
{
    public class PointArray : IEnumerable<Point>
    {
        private Point[] innerArray;

        public int Length
        {
            get { return innerArray.Length; }
        }

        public Point this[int index]
        {
            get { return innerArray[index]; }
            set { innerArray[index] = value; }
        }

        public PointArray(Point[] innerArray)
        {
            this.innerArray = innerArray;
        }

        public PointArray(int capacity)
        {
            this.innerArray = new Point[capacity];
        }

        public double GetDistance(int pointIndex1, int pointIndex2)
        {
            return Point.Distance(innerArray[pointIndex1], innerArray[pointIndex2]);
        }

        public double GetDistance(IEnumerable<int> indices)
        {
            double totalDistance = 0;
            int prevIndex = -1;

            foreach (int index in indices)
            {
                int currentIndex = index;

                if (prevIndex > -1)
                    totalDistance += Point.Distance(innerArray[prevIndex], innerArray[currentIndex]);

                prevIndex = currentIndex;
            }

            return totalDistance;
        }

        public IEnumerator<Point> GetEnumerator()
        {
            foreach (var point in innerArray)
                yield return point;
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
