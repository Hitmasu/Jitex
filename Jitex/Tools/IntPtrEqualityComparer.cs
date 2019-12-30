using System;
using System.Collections.Generic;
using System.Text;

namespace Jitex.Tools
{
    internal class IntPtrEqualityComparer : IEqualityComparer<IntPtr>
    {
        public static readonly IntPtrEqualityComparer Instance = new IntPtrEqualityComparer();

        public bool Equals(IntPtr x, IntPtr y)
        {
            return x == y;
        }

        public int GetHashCode(IntPtr obj)
        {
            return obj.GetHashCode();
        }
    }
}