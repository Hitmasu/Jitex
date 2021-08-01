using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Jitex.Exceptions;
using Jitex.Framework;
using Jitex.Framework.Offsets;

namespace Jitex.JIT.CorInfo
{
    internal static class CEEInfo
    {
        private static IntPtr CEEInfoVTable => RuntimeFramework.Framework.CEEInfoVTable;

        private static readonly ConstructStringLiteralDelegate _constructStringLiteral;

        private static readonly ResolveTokenDelegate _resolveToken;

        public static IntPtr ResolveTokenIndex { get; }

        public static IntPtr ConstructStringLiteralIndex { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate void ResolveTokenDelegate(IntPtr thisHandle, IntPtr pResolvedToken);

        [UnmanagedFunctionPointer(default)]
        public delegate InfoAccessType ConstructStringLiteralDelegate(IntPtr thisHandle, IntPtr hModule, int metadataToken, IntPtr ptrString);

        static CEEInfo()
        {
            if (CEEInfoVTable == IntPtr.Zero)
                throw new VTableNotLoaded(nameof(CEEInfo));

            ResolveTokenIndex = CEEInfoVTable + IntPtr.Size * CEEInfoOffset.ResolveToken;
            ConstructStringLiteralIndex = CEEInfoVTable + IntPtr.Size * CEEInfoOffset.ConstructStringLiteral;

            IntPtr resolveTokenPtr = Marshal.ReadIntPtr(ResolveTokenIndex);
            IntPtr constructStringLiteralPtr = Marshal.ReadIntPtr(ConstructStringLiteralIndex);

            _resolveToken = Marshal.GetDelegateForFunctionPointer<ResolveTokenDelegate>(resolveTokenPtr);
            _constructStringLiteral = Marshal.GetDelegateForFunctionPointer<ConstructStringLiteralDelegate>(constructStringLiteralPtr);

            //PrepareMethod in .NET Core 2.0, will raise StackOverFlowException
            ResolveToken(default, default);

            System.Reflection.MethodInfo constructString = typeof(CEEInfo).GetMethod(nameof(ConstructStringLiteral))!;
            RuntimeHelpers.PrepareMethod(constructString.MethodHandle);
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
    }
}