using System;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace Jitex.Utils
{
    /// <summary>
    /// Utilities for type.
    /// </summary>
    internal static class TypeHelper
    {
        private static readonly IDictionary<Type, int> CacheSizeOf = new Dictionary<Type, int>();

        public static int SizeOf(Type type)
        {
            if (type == typeof(void))
                throw new ArgumentException("Type Void can't be used in SizeOf.");

            if (!CacheSizeOf.TryGetValue(type, out int size))
            {
                Func<int> getSizeOf = CreateSizeOfMethod(type);
                size = getSizeOf();
                CacheSizeOf.Add(type, getSizeOf());
            }

            return size;
        }

        private static Func<int> CreateSizeOfMethod(Type type)
        {
            DynamicMethod dm = new DynamicMethod($"SizeOfFrom{type.Name}", typeof(int), Type.EmptyTypes);
            ILGenerator generator = dm.GetILGenerator();
            generator.Emit(OpCodes.Sizeof, type);
            generator.Emit(OpCodes.Ret);

            return (Func<int>)dm.CreateDelegate(typeof(Func<int>));
        }
    }
}