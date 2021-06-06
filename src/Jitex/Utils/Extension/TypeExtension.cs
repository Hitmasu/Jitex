using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Jitex.Utils.Extension
{
    internal static class TypeExtension
    {
        private static readonly Type CanonType;

        static TypeExtension()
        {
            CanonType = Type.GetType("System.__Canon")!;
        }

        public static bool IsAwaitable(this Type type)
        {
            if (type.IsGenericType)
                type = type.GetGenericTypeDefinition();

            return type.IsTask() || type.IsValueTask();
        }

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CanBeInline(this Type type) => IsStruct(type) && SizeOf(type) <= IntPtr.Size;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsStruct(this Type type) => type != typeof(void) && type.IsValueType && !type.IsEnum;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SizeOf(this Type type) => TypeHelper.SizeOf(type);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsCanon(this Type type) => !type.IsPrimitive;

        public static IntPtr GetValueAddress(this Type type, IntPtr address, bool isDirectAddress = false)
        {
            Type elementType = type.IsByRef ? type.GetElementType()! : type;

            if (elementType.IsPrimitive)
            {
                if (type.IsByRef)
                    return address;

                return Marshal.ReadIntPtr(address);
            }

            if (type.CanBeInline())
            {
                IntPtr valueAddress = Marshal.ReadIntPtr(address);

                if (isDirectAddress)
                    return valueAddress;

                return Marshal.ReadIntPtr(valueAddress + IntPtr.Size);
            }

            if (elementType.IsValueType)
            {
                address = Marshal.ReadIntPtr(address);
                return address + IntPtr.Size;
            }

            if (type.IsByRef)
                return address;

            return Marshal.ReadIntPtr(address);
        }
    }
}
