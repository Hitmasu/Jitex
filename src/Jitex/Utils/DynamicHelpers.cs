using System;
using System.Reflection;
using System.Reflection.Emit;

namespace Jitex.Utils
{
    internal static class DynamicHelpers
    {
        private static readonly FieldInfo? m_owner;
        private static readonly Type _rtDynamicMethod;

        static DynamicHelpers()
        {
            _rtDynamicMethod = Type.GetType("System.Reflection.Emit.DynamicMethod+RTDynamicMethod");
            m_owner = _rtDynamicMethod.GetField("m_owner", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        public static bool IsDynamicScope(IntPtr scope)
        {
            if (OSHelper.IsX86)
                return false;

            if (OSHelper.IsX86)
                return (scope.ToInt32() & 1) == 1;

            return (scope.ToInt64() & 1) == 1;
        }

        public static DynamicMethod GetOwner(MethodBase method)
        {
            return (DynamicMethod)m_owner.GetValue(method);
        }

        public static bool IsRTDynamicMethod(MethodBase method)
        {
            return method.GetType() == _rtDynamicMethod;
        }
    }
}