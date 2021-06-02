using Jitex.Utils.Extension;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Jitex.Utils
{
    /// <summary>
    /// Utilities for type.
    /// </summary>
    internal static class TypeHelper
    {
        private static readonly MethodInfo GetTypeFromHandleUnsafe;
        private static readonly IDictionary<Type, int> CacheSizeOf = new Dictionary<Type, int>();

        static TypeHelper()
        {
            GetTypeFromHandleUnsafe = typeof(Type).GetMethod("GetTypeFromHandleUnsafe", BindingFlags.Static | BindingFlags.NonPublic);
        }

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

        public static bool HasCanon(Type type)
        {
            return type is { IsGenericType: true } && type.GetGenericArguments().Any(w => w.IsCanon());
        }

        public static Type GetTypeFromHandle(IntPtr handle)
        {
            return (Type)GetTypeFromHandleUnsafe.Invoke(null, new object[] { handle });
        }
    }
}