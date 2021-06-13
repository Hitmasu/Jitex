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
            
            bool xHasCanon = MethodHelper.HasCannon(x);

            if (x.DeclaringType != null)
                xHasCanon |= TypeHelper.HasCanon(x.DeclaringType);

            bool yHasCanon = MethodHelper.HasCannon(y);

            if (y.DeclaringType != null)
                yHasCanon |= TypeHelper.HasCanon(y.DeclaringType);

            if (xHasCanon != yHasCanon)
                return false;

            if (xHasCanon && x is MethodInfo xMethodInfo)
                x = MethodHelper.GetBaseMethodGeneric(xMethodInfo);

            if (yHasCanon && y is MethodInfo yMethodInfo)
                y = MethodHelper.GetBaseMethodGeneric(yMethodInfo);

            return x == y;
        }

        public int GetHashCode(MethodBase obj)
        {
            return obj.GetHashCode();
        }
    }
}