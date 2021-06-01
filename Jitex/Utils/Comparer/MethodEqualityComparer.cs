using System.Collections.Generic;
using System.Reflection;

namespace Jitex.Utils.Comparer
{
    internal class MethodEqualityComparer : IEqualityComparer<MethodBase>
    {
        public static MethodEqualityComparer Instance => new MethodEqualityComparer();

        public bool Equals(MethodBase x, MethodBase y)
        {
            if (x == null && y == null)
                return true;

            if (x == null || y == null)
                return false;

            if (x.IsGenericMethod != y.IsGenericMethod)
                return false;

            if(x.IsGenericMethod)
                x = MethodHelper.GetBaseMethodGeneric((MethodInfo)x);

            if(y.IsGenericMethod)
                y = MethodHelper.GetBaseMethodGeneric((MethodInfo)y);

            return x == y;
        }

        public int GetHashCode(MethodBase obj)
        {
            return obj.GetHashCode();
        }
    }
}
