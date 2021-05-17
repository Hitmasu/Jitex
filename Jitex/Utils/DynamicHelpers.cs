using System;
using System.Reflection;
using System.Reflection.Emit;

namespace Jitex.Utils
{
    internal static class DynamicHelpers
    {
        private static readonly FieldInfo m_owner;

        static DynamicHelpers()
        {
            Type? type = Type.GetType("System.Reflection.Emit.DynamicMethod+RTDynamicMethod");
            m_owner = type.GetField("m_owner", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        public static bool IsDynamicScope(IntPtr scope) => (scope.ToInt64() & 1) == 1;

        public static DynamicMethod GetOwner(MethodBase method)
        {
            return (DynamicMethod) m_owner.GetValue(method);
        }
    }
}
