using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TSP
{
    public interface ISplittableSet<T> : IEnumerable<T>
    {
        int ItemsCount { get; }
        IEnumerable<IEnumerator<T>> GetDisjointEnumerators(int enumeratorsCount);
    }
}
