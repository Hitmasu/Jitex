using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;

namespace Jitex.Utils.Comparer
{
    internal class MethodEqualityComparer : IEqualityComparer<MethodBase>
    {
        private readonly ConcurrentDictionary<IntPtr, IntPtr> _methodCache = new ConcurrentDictionary<IntPtr, IntPtr>();
        public static MethodEqualityComparer Instance => new MethodEqualityComparer();

        public bool Equals(MethodBase x, MethodBase y)
        {
            if (x == null && y == null)
                return true;

            if (x == null || y == null)
                return false;

            IntPtr xOriginalMethodHandle = MethodHelper.GetMethodHandle(x).Value;
            IntPtr yOriginalMethodHandle = MethodHelper.GetMethodHandle(y).Value;

            if (!_methodCache.TryGetValue(xOriginalMethodHandle, out IntPtr xMethodHandle))
            {
                bool xHasCanon = MethodHelper.HasCanon(x);

                if (!xHasCanon && x.DeclaringType != null)
                    xHasCanon = TypeHelper.HasCanon(x.DeclaringType);

                if (xHasCanon && x is MethodInfo xMethodInfo)
                {
                    x = MethodHelper.GetBaseMethodGeneric(xMethodInfo);
                    xMethodHandle = x.MethodHandle.Value;
                }
                else
                {
                    xMethodHandle = xOriginalMethodHandle;
                }

                _methodCache.TryAdd(xOriginalMethodHandle, xMethodHandle);
            }

            if (!_methodCache.TryGetValue(yOriginalMethodHandle, out IntPtr yMethodHandle))
            {
                bool yHasCanon = MethodHelper.HasCanon(y);

                if (!yHasCanon && y.DeclaringType != null)
                    yHasCanon = TypeHelper.HasCanon(y.DeclaringType);

                if (yHasCanon && y is MethodInfo yMethodInfo)
                {
                    y = MethodHelper.GetBaseMethodGeneric(yMethodInfo);
                    yMethodHandle = y.MethodHandle.Value;
                }
                else
                {
                    yMethodHandle = yOriginalMethodHandle;
                }

                _methodCache.TryAdd(yOriginalMethodHandle, yMethodHandle);
            }

            return xMethodHandle == yMethodHandle;
        }

        public int GetHashCode(MethodBase obj)
        {
            return obj.GetHashCode();
        }
    }
}