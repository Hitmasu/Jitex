using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Jitex.Exceptions;
using Jitex.Runtime;
using Jitex.Runtime.Offsets;

namespace Jitex.JIT.CorInfo
{
    internal static class CEEInfo
    {
        private static IntPtr CEEInfoVTable => RuntimeFramework.GetFramework().CEEInfoVTable;

        private static readonly GetMethodDefFromMethodDelegate _getMethodDefFromMethod;

        private static readonly ConstructStringLiteralDelegate _constructStringLiteral;

        private static readonly ResolveTokenDelegate _resolveToken;

        private static readonly GetFunctionEntryPointDelegate _getFunctionEntryPoint;

        public static IntPtr ResolveTokenIndex { get; }

        public static IntPtr ConstructStringLiteralIndex { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate uint GetMethodDefFromMethodDelegate(IntPtr thisHandle, IntPtr hMethod);

        [UnmanagedFunctionPointer(default)]
        public delegate void ResolveTokenDelegate(IntPtr thisHandle, IntPtr pResolvedToken);

        [UnmanagedFunctionPointer(default)]
        public delegate InfoAccessType ConstructStringLiteralDelegate(IntPtr thisHandle, IntPtr hModule, int metadataToken, IntPtr ptrString);

        [UnmanagedFunctionPointer(default)]
        public delegate InfoAccessType GetFunctionEntryPointDelegate(IntPtr thisHandle, IntPtr hMethod, out IntPtr pResult);

        static CEEInfo()
        {
            if (CEEInfoVTable == IntPtr.Zero)
                throw new VTableNotLoaded(nameof(CEEInfo));

            IntPtr getMethodDefFromMethodIndex = CEEInfoVTable + IntPtr.Size * CEEInfoOffset.GetMethodDefFromMethod;
            IntPtr getFunctionEntryPointPtr = CEEInfoVTable + IntPtr.Size * CEEInfoOffset.GetFunctionEntryPoint;

            ResolveTokenIndex = CEEInfoVTable + IntPtr.Size * CEEInfoOffset.ResolveToken;
            ConstructStringLiteralIndex = CEEInfoVTable + IntPtr.Size * CEEInfoOffset.ConstructStringLiteral;

            IntPtr resolveTokenPtr = Marshal.ReadIntPtr(ResolveTokenIndex);
            IntPtr getMethodDefFromMethodPtr = Marshal.ReadIntPtr(getMethodDefFromMethodIndex);
            IntPtr constructStringLiteralPtr = Marshal.ReadIntPtr(ConstructStringLiteralIndex);
            IntPtr getFuncitonEntryPointPtr = Marshal.ReadIntPtr(getFunctionEntryPointPtr);

            _getMethodDefFromMethod = Marshal.GetDelegateForFunctionPointer<GetMethodDefFromMethodDelegate>(getMethodDefFromMethodPtr);
            _resolveToken = Marshal.GetDelegateForFunctionPointer<ResolveTokenDelegate>(resolveTokenPtr);
            _constructStringLiteral = Marshal.GetDelegateForFunctionPointer<ConstructStringLiteralDelegate>(constructStringLiteralPtr);
            _getFunctionEntryPoint = Marshal.GetDelegateForFunctionPointer<GetFunctionEntryPointDelegate>(getFuncitonEntryPointPtr);

            //PrepareMethod in .NET Core 2.0, will raise StackOverFlowException
            ResolveToken(default, default);

            System.Reflection.MethodInfo constructString = typeof(CEEInfo).GetMethod(nameof(ConstructStringLiteral))!;
            RuntimeHelpers.PrepareMethod(constructString.MethodHandle);
        }

        public static uint GetMethodDefFromMethod(IntPtr hMethod)
        {
            return _getMethodDefFromMethod(CEEInfoVTable, hMethod);
        }

        public static void ResolveToken(IntPtr thisHandle, IntPtr pResolvedToken)
        {
            if (thisHandle == IntPtr.Zero)
                return;

            _resolveToken(thisHandle, pResolvedToken);
        }

        public static InfoAccessType ConstructStringLiteral(IntPtr thisHandle, IntPtr hModule, int metadataToken, IntPtr ptrString)
        {
            return _constructStringLiteral(thisHandle, hModule, metadataToken, ptrString);
        }

        public static IntPtr GetFunctionEntryPoint(IntPtr hMethod)
        {
            _getFunctionEntryPoint(CEEInfoVTable, hMethod, out IntPtr result);
            return result;
        }
    }
}