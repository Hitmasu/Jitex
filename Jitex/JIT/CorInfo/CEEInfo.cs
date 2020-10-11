using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Jitex.JIT.CorInfo
{
    internal class CEEInfo
    {
        private readonly IntPtr _corJitInfo;

        private readonly GetMethodDefFromMethodDelegate _getMethodDefFromMethod;

        private readonly ConstructStringLiteralDelegate _constructStringLiteral;

        private readonly ResolveTokenDelegate _resolveToken;

        public IntPtr ResolveTokenIndex { get; }

        public IntPtr ConstructStringLiteralIndex { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate uint GetMethodDefFromMethodDelegate(IntPtr thisHandle, IntPtr hMethod);

        [UnmanagedFunctionPointer(default)]
        public delegate void ResolveTokenDelegate(IntPtr thisHandle, ref CORINFO_RESOLVED_TOKEN pResolvedToken);

        [UnmanagedFunctionPointer(default)]
        public delegate InfoAccessType ConstructStringLiteralDelegate(IntPtr thisHandle, IntPtr hModule, int metadataToken, IntPtr ptrString);

        public CEEInfo(IntPtr corJitInfo)
        {
            _corJitInfo = corJitInfo;

            Version version = new Version(3, 1, 1);

            IntPtr getMethodDefFromMethodIndex = IntPtr.Zero;

            if (Environment.Version >= version)
            {
                ResolveTokenIndex = _corJitInfo + IntPtr.Size * 0x1C;
                getMethodDefFromMethodIndex = _corJitInfo + IntPtr.Size * 0x74;
                ConstructStringLiteralIndex = _corJitInfo + IntPtr.Size * 0x97;
            }

            IntPtr resolveTokenPtr = Marshal.ReadIntPtr(ResolveTokenIndex);
            IntPtr getMethodDefFromMethodPtr = Marshal.ReadIntPtr(getMethodDefFromMethodIndex);
            IntPtr constructStringLiteralPtr = Marshal.ReadIntPtr(ConstructStringLiteralIndex);

            _getMethodDefFromMethod = Marshal.GetDelegateForFunctionPointer<GetMethodDefFromMethodDelegate>(getMethodDefFromMethodPtr);
            _resolveToken = Marshal.GetDelegateForFunctionPointer<ResolveTokenDelegate>(resolveTokenPtr);
            _constructStringLiteral = Marshal.GetDelegateForFunctionPointer<ConstructStringLiteralDelegate>(constructStringLiteralPtr);


            MethodInfo resolveTokenMethod = GetType().GetMethod(nameof(ResolveToken), BindingFlags.Instance | BindingFlags.Public);
            RuntimeHelpers.PrepareMethod(resolveTokenMethod.MethodHandle);
        }

        public uint GetMethodDefFromMethod(IntPtr hMethod)
        {
            return _getMethodDefFromMethod(_corJitInfo, hMethod);
        }

        public void ResolveToken(IntPtr thisHandle, ref CORINFO_RESOLVED_TOKEN pResolvedToken)
        {
            _resolveToken(thisHandle, ref pResolvedToken);
        }

        public InfoAccessType ConstructStringLiteral(IntPtr thisHandle, IntPtr hModule, int metadataToken, IntPtr ptrString)
        {
            return _constructStringLiteral(thisHandle, hModule, metadataToken, ptrString);
        }
    }
}