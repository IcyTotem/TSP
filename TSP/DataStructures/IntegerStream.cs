using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace TSP
{
    public class IntegerStream : IEnumerable<int>
    {
        private string filename;

        public IntegerStream(string filename)
        {
            this.filename = filename;
        }

        public IEnumerator<int> GetEnumerator()
        {
            return new StreamEnumerator(new StreamReader(this.filename));
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }


        private class StreamEnumerator : IEnumerator<int>
        {
            private StreamReader reader;
            private int current;

            public StreamEnumerator(StreamReader reader)
            {
                this.reader = reader;
            }

            public int Current
            {
                get { return current; }
            }

            object System.Collections.IEnumerator.Current
            {
                get { return this.Current; }
            }

            public void Dispose()
            {
                reader.Dispose();
            }

            public bool MoveNext()
            {
                if (reader.EndOfStream)
                    return false;

                current = int.Parse(reader.ReadLine());
                return true;
            }

            public void Reset()
            {
                reader.BaseStream.Seek(0, SeekOrigin.Begin);
                reader.DiscardBufferedData();
            }
        }
    }
}
