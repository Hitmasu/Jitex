using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using MethodInfo = Jitex.JIT.CorInfo.MethodInfo;

namespace Jitex.Utils
{
    internal static class DynamicHelpers
    {
        private static FieldInfo m_owner;

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
