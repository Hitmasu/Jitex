using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Jitex.Utils.Extension
{
    internal static class TypeExtension
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsStruct(this Type type) => type != typeof(void) && type.IsValueType && !type.IsEnum;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsCanon(this Type type) => !type.IsPrimitive;

        public static bool IsAwaitable(this Type type) => type.IsTask() || type.IsValueTask();

        public static bool IsTask(this Type type)
        {
            if (type == typeof(Task))
                return true;

            if (!type.IsGenericType)
                return false;

            return type.GetGenericTypeDefinition() == typeof(Task<>);
        }

        public static bool IsValueTask(this Type type)
        {
            if (type == typeof(ValueTask))
                return true;

            if (!type.IsGenericType)
                return false;

            return type.GetGenericTypeDefinition() == typeof(ValueTask<>);
        }
    }
}