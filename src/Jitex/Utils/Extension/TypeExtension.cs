using System;
using System.Runtime.CompilerServices;

namespace Jitex.Utils.Extension
{
    internal static class TypeExtension
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsStruct(this Type type) => type != typeof(void) && type.IsValueType && !type.IsEnum;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsCanon(this Type type) => !type.IsPrimitive;
    }
}