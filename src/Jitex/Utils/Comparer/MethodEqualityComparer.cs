using System.Collections.Generic;
using System.Reflection;

namespace Jitex.Utils.Comparer
{
    internal class MethodEqualityComparer : IEqualityComparer<MethodBase>
    {
        public static MethodEqualityComparer Instance => new MethodEqualityComparer();

        public bool Equals(MethodBase x, MethodBase y) => x == y;

        public int GetHashCode(MethodBase obj)
        {
            return obj.GetHashCode();
        }
    }
}