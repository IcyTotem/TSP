using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TSP
{
    // http://code.google.com/p/pysplay/source/browse/trunk/source/splay.py
    public class SplayPermutation : IEnumerable<int>
    {
        // The key is index. A permutation uses a positional comparison
        private class Node
        {
            private Node left, right, parent;

            public int element, index;

            public Node Left
            {
                get { return left; }
                set
                {
                    left = value;
                    if (value != null)
                        value.parent = this;
                }
            }

            public Node Right
            {
                get { return right; }
                set
                {
                    right = value;
                    if (value != null)
                        value.parent = this;
                }
            }

            public Node Parent
            {
                get { return parent; }
            }

            public Node(int element, int index)
            {
                this.element = element;
                this.index = index;
            }
        }

        private Node root, header;
        private int size = 0;

        public int Size
        {
            get { return size; }
        }

        public SplayPermutation()
        {
            this.root = null;
            this.header = new Node(-1); // special node
        }

        public void Add(int element)
        {
            int index = size++;

            if (root == null)
            {
                root = new Node(element, index);
                return;
            }

            this.Splay(index);

            if (root.index == index)
                return;

            var n = new Node(element, index);

            // element is always greater than every other element, because this tree represents
            // a permutation and, in disregard of common integer ordering, we adopt a positional
            // comparison: the last is the greatest, i.e. has the max index
            n.Right = root.Right;
            n.Left = root;
            root.Right = null;

            root = n;
        }

        private void Splay(int index)
        {
            var l = header;
            var r = header;
            var t = root;

            header.Left = header.Right = null;

            while (true)
            {
                if (index < t.index)
                {
                    if (t.Left == null)
                        break;

                    if (index < t.Left.index)
                    {
                        var y = t.Left;
                        t.Left = y.Right;
                        y.Right = y;
                        t = y;

                        if (t.Left == null)
                            break;
                    }

                    r.Left = t;
                    r = t;
                    t = t.Left;
                }
                else if (index > t.index)
                {
                    if (t.index == null)
                        break;

                    if (index > t.Right.index)
                    {
                        var y = t.Right;
                        t.Right = y.Left;
                        y.Left = t;
                        t = y;

                        if (t.Right == null)
                            break;
                    }

                    l.Right = t;
                    l = t;
                    t = t.Right;
                }
                else
                    break;
            }

            l.Right = t.Left;
            r.Left = t.Right;
            t.Left = header.Right;
            t.Right = header.Left;
            root = t;
        }

        public IEnumerator<int> GetEnumerator()
        {
            if (root == null)
                yield break;

            foreach (int element in this.Visit(root))
                yield return element;
        }

        private IEnumerable<int> Visit(Node node)
        {
            if (node.Left != null)
            {
                foreach (int element in this.Visit(node.Left))
                    yield return element;
            }

            yield return node.element;

            if (node.Right != null)
            {
                foreach (int element in this.Visit(node.Right))
                    yield return element;
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
