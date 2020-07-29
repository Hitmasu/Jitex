using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Jitex.JIT.CorInfo
{
    internal class CEEInfo
    {
        private readonly IntPtr _corJitInfo;

        private readonly GetMethodModuleDelegate _getMethodModule;

        private readonly GetMethodDefFromMethodDelegate _getMethodDefFromMethod;

        private ConstructStringLiteralDelegate _constructStringLiteral;
        private ResolveTokenDelegate _resolveToken;

        public IntPtr ResolveTokenIndex { get; }

        public IntPtr ConstructStringLiteralIndex { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate uint GetMethodDefFromMethodDelegate(IntPtr thisHandle, IntPtr hMethod);

        [UnmanagedFunctionPointer(default)]
        public delegate IntPtr GetMethodModuleDelegate(IntPtr thisHandle, IntPtr hMethod);

        [UnmanagedFunctionPointer(default)]
        public delegate void ResolveTokenDelegate(IntPtr thisHandle, ref CORINFO_RESOLVED_TOKEN pResolvedToken);

        [UnmanagedFunctionPointer(default)]
        public delegate InfoAccessType ConstructStringLiteralDelegate(IntPtr thisHandle, IntPtr hModule, int metadataToken, IntPtr ptrString);

        public CEEInfo(IntPtr corJitInfo)
        {
            _corJitInfo = corJitInfo;

            Version version = new Version(3, 1, 1);

            IntPtr getMethodModuleIndex = IntPtr.Zero;
            IntPtr getMethodDefFromMethodIndex = IntPtr.Zero;

            if (Environment.Version >= version)
            {
                getMethodModuleIndex = _corJitInfo + IntPtr.Size * 0xA;
                ResolveTokenIndex = _corJitInfo + IntPtr.Size * 0x1C;
                getMethodDefFromMethodIndex = _corJitInfo + IntPtr.Size * 0x74;
                ConstructStringLiteralIndex = _corJitInfo + IntPtr.Size * 0x97;
            }

            IntPtr getMethodModulePtr = Marshal.ReadIntPtr(getMethodModuleIndex);
            IntPtr resolveTokenPtr = Marshal.ReadIntPtr(ResolveTokenIndex);
            IntPtr getMethodDefFromMethodPtr = Marshal.ReadIntPtr(getMethodDefFromMethodIndex);
            IntPtr constructStringLiteralPtr = Marshal.ReadIntPtr(ConstructStringLiteralIndex);

            _getMethodModule = Marshal.GetDelegateForFunctionPointer<GetMethodModuleDelegate>(getMethodModulePtr);
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

        public IntPtr GetMethodModule(IntPtr hMethod)
        {
            return _getMethodModule(_corJitInfo, hMethod);
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