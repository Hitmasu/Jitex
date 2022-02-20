using Jitex.Utils.Extension;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace Jitex.Utils
{
    /// <summary>
    /// Utilities for type.
    /// </summary>
    internal static class TypeHelper
    {
        public static Type CanonType { get; }

        private static readonly IDictionary<Type, int> CacheSizeOf = new Dictionary<Type, int>();

        static TypeHelper()
        {
            CanonType = Type.GetType("System.__Canon");
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
            DynamicMethod dm = new($"SizeOfFrom{type.Name}", typeof(int), Type.EmptyTypes);
            ILGenerator generator = dm.GetILGenerator();
            generator.Emit(OpCodes.Sizeof, type);
            generator.Emit(OpCodes.Ret);

            return (Func<int>) dm.CreateDelegate(typeof(Func<int>));
        }

        internal static bool HasCanon(Type? type, bool ignoreCanonType = false)
        {
            if (type is not {IsGenericType: true})
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

        internal static Type GetBaseTypeGeneric(Type type)
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