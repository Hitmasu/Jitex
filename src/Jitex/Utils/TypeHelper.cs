using Jitex.Utils.Extension;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using RuntimeTypeHandle = System.RuntimeTypeHandle;

namespace Jitex.Utils
{
    /// <summary>
    /// Utilities for type.
    /// </summary>
    internal static class TypeHelper
    {
        private static readonly Type CanonType;
        private static readonly MethodInfo GetTypeFromHandleUnsafe;
        private static readonly IDictionary<Type, int> CacheSizeOf = new Dictionary<Type, int>();

        static TypeHelper()
        {
            CanonType = Type.GetType("System.__Canon");
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

        internal static bool HasCanon(Type? type, bool ignoreCanonType = false)
        {
            if (type == null)
                return false;

            if (!type.IsGenericType)
                return false;

            Type[] types = type.GetGenericArguments();

            bool hasCanon = false;

            foreach (Type t in types)
            {
                if (ignoreCanonType && t == CanonType)
                    continue;

                if (t.IsCanon())
                {
                    hasCanon = true;
                    break;
                }
            }

            return hasCanon;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsGeneric(Type? type) => type is { IsGenericType: true };

        public static Type GetTypeFromHandle(IntPtr handle)
        {
            return (Type)GetTypeFromHandleUnsafe.Invoke(null, new object[] { handle });
        }

        public static Type GetBaseTypeGeneric(Type type)
        {
            if (!type.IsGenericType)
                return type;

            Type[] genericArguments = type.GetGenericArguments();
            bool hasCanon = false;

            for (int i = 0; i < genericArguments.Length; i++)
            {
                Type genericArgument = genericArguments[i];

                if (genericArgument.IsCanon())
                {
                    hasCanon = true;
                    genericArguments[i] = CanonType;
                }
            }

            if (hasCanon)
                type = type.GetGenericTypeDefinition().MakeGenericType(genericArguments);

            return type;
        }
    }
}