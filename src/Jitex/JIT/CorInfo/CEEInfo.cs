using System;
using System.Diagnostics;
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
        private static readonly GetEhInfoDelegate _getEHInfo;

        public static IntPtr ResolveTokenIndex { get; }
        public static IntPtr GetEHInfoIndex { get; set; }
        public static IntPtr ConstructStringLiteralIndex { get; }

        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        public delegate void ResolveTokenDelegate(IntPtr thisHandle, IntPtr pResolvedToken);

        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        public delegate InfoAccessType ConstructStringLiteralDelegate(IntPtr thisHandle, IntPtr hModule,
            int metadataToken, IntPtr ptrString);

        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        public delegate void GetEhInfoDelegate(IntPtr thisHandle, IntPtr ftn, uint ehNumber, out IntPtr clause);
        
        static CEEInfo()
        {
            if (CEEInfoVTable == IntPtr.Zero)
                throw new VTableNotLoaded(nameof(CEEInfo));

            GetEHInfoIndex = CEEInfoVTable + IntPtr.Size * CEEInfoOffset.GetEHInfo;
            ResolveTokenIndex = CEEInfoVTable + IntPtr.Size * CEEInfoOffset.ResolveToken;
            ConstructStringLiteralIndex = CEEInfoVTable + IntPtr.Size * CEEInfoOffset.ConstructStringLiteral;

            var resolveTokenPtr = Marshal.ReadIntPtr(ResolveTokenIndex);
            var getEhInfoPtr = Marshal.ReadIntPtr(GetEHInfoIndex);
            var constructStringLiteralPtr = Marshal.ReadIntPtr(ConstructStringLiteralIndex);

            _resolveToken = Marshal.GetDelegateForFunctionPointer<ResolveTokenDelegate>(resolveTokenPtr);
            _getEHInfo = Marshal.GetDelegateForFunctionPointer<GetEhInfoDelegate>(getEhInfoPtr);
            _constructStringLiteral =
                Marshal.GetDelegateForFunctionPointer<ConstructStringLiteralDelegate>(constructStringLiteralPtr);

            //PrepareMethod in .NET Core 2.0, will raise StackOverFlowException
            ResolveToken(default, default);

            var constructString = typeof(CEEInfo).GetMethod(nameof(ConstructStringLiteral))!;
            RuntimeHelpers.PrepareMethod(constructString.MethodHandle);
        }

        public static void GetEHInfo(IntPtr thisHandle, IntPtr ftn, uint ehNumber, out IntPtr clause)
        {
            _getEHInfo(thisHandle, ftn, ehNumber, out clause);
        }

        public static void ResolveToken(IntPtr thisHandle, IntPtr pResolvedToken)
        {
            if (thisHandle == IntPtr.Zero)
                return;

            _resolveToken(thisHandle, pResolvedToken);
        }

        public static InfoAccessType ConstructStringLiteral(IntPtr thisHandle, IntPtr hModule, int metadataToken,
            IntPtr ptrString)
        {
            return _constructStringLiteral(thisHandle, hModule, metadataToken, ptrString);
        }
    }
}