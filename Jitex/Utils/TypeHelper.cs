using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace Jitex.Utils
{
    /// <summary>
    /// Utilities for type.
    /// </summary>
    public static class TypeHelper
    {
        private static readonly IDictionary<Type, int> CacheSizeOf = new Dictionary<Type, int>();

        public static int SizeOf(Type type)
        {
            if (!CacheSizeOf.TryGetValue(type, out int size))
            {
                Func<int> getSizeOf = CreateSizeOfMethod(type);
                size = getSizeOf();
                CacheSizeOf.Add(type, getSizeOf());
            }

            return size;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsStruct(Type type) => type.IsValueType && !type.IsEnum;

        private static Func<int> CreateSizeOfMethod(Type type)
        {
            DynamicMethod dm = new DynamicMethod($"SizeOfFrom{type.Name}", typeof(int), Type.EmptyTypes);
            ILGenerator generator = dm.GetILGenerator();
            generator.Emit(OpCodes.Sizeof, type);
            generator.Emit(OpCodes.Ret);

            return (Func<int>)dm.CreateDelegate(typeof(Func<int>));
        }

        public static Type GetTypeFromHandle(IntPtr handle)
        {
            TypedReference tr = default;
            Span<IntPtr> spanTr;

            unsafe
            {
                spanTr = new Span<IntPtr>(&tr, sizeof(TypedReference));
            }

            spanTr[1] = handle;

            return __reftype(tr);
        }
    }
}