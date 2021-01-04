using System;
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

            if (x.MetadataToken != y.MetadataToken)
                return false;

            if (!x.IsGenericMethod)
                return x == y;

            x = MethodHelper.GetMethodGeneric((MethodInfo)x);
            y = MethodHelper.GetMethodGeneric((MethodInfo)y);

            return x == y;
        }

        public int GetHashCode(MethodBase obj)
        {
            return obj.GetHashCode();
        }
    }
}
