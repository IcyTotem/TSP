using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TSP
{
    public struct Point
    {
        public int X, Y;

        public Point(int x, int y) { this.X = x; this.Y = y; }

        public static int SquareDistance(Point i, Point j)
        {
            int dx = (i.X - j.X);
            int dy = (i.Y - j.Y);

            return dx * dx + dy * dy;
        }

        public static double Distance(Point i, Point j)
        {
            return Math.Sqrt(Point.SquareDistance(i, j));
        }
    }
}
