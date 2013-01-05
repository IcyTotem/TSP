using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TSP
{
    public struct Edge
    {
        public int Head, Tail;

        public Edge(int head, int tail) { this.Head = head; this.Tail = tail; }
    }
}
