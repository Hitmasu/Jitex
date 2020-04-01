using System;
using System.Runtime.InteropServices;

namespace Jitex.JIT.CorInfo
{
    internal class CEEInfo
    {
        private readonly IntPtr _corJitInfo;

        [UnmanagedFunctionPointer(default)]
        public delegate IntPtr GetMethodClassDelegate(IntPtr thisHandle, IntPtr hMethod);

        private readonly GetMethodClassDelegate _getMethodClass;

        [UnmanagedFunctionPointer(default)]
        public delegate IntPtr GetMethodModuleDelegate(IntPtr thisHandle, IntPtr hMethod);

        private readonly GetMethodModuleDelegate _getMethodModule;

        [UnmanagedFunctionPointer(default)]
        public delegate void ResolveTokenDelegate(IntPtr thisHandle, ref CORINFO_RESOLVED_TOKEN pResolvedToken);

        [UnmanagedFunctionPointer(default)]
        public delegate uint GetMethodDefFromMethodDelegate(IntPtr thisHandle, IntPtr hMethod);

        private GetMethodDefFromMethodDelegate _getMethodDefFromMethod;


        //ResolveToken is to be hooked by our jit.
        public ResolveTokenDelegate ResolveToken;
        public IntPtr ResolveTokenIndex { get; }


        public CEEInfo(IntPtr corJitInfo)
        {
            _corJitInfo = corJitInfo;

            string clrVersion = Environment.Version.ToString();

            IntPtr getMethodClassIndex = IntPtr.Zero;
            IntPtr getMethodModuleIndex = IntPtr.Zero;
            IntPtr getMethodDefFromMethodIndex = IntPtr.Zero;

            switch (clrVersion)
            {
                case "3.1.1":
                    getMethodClassIndex = _corJitInfo + IntPtr.Size * 9;
                    getMethodModuleIndex = _corJitInfo + IntPtr.Size * 10;
                    ResolveTokenIndex = _corJitInfo + IntPtr.Size * 28;
                    getMethodDefFromMethodIndex = _corJitInfo + IntPtr.Size * 116;
                    break;
            }

            IntPtr getMethodClassPtr = Marshal.ReadIntPtr(getMethodClassIndex);
            IntPtr getMethodModulePtr = Marshal.ReadIntPtr(getMethodModuleIndex);
            IntPtr resolveTokenPtr = Marshal.ReadIntPtr(ResolveTokenIndex);
            IntPtr getMethodDefFromMethodPtr = Marshal.ReadIntPtr(getMethodDefFromMethodIndex);

            _getMethodClass = Marshal.GetDelegateForFunctionPointer<GetMethodClassDelegate>(getMethodClassPtr);
            _getMethodModule = Marshal.GetDelegateForFunctionPointer<GetMethodModuleDelegate>(getMethodModulePtr);
            _getMethodDefFromMethod = Marshal.GetDelegateForFunctionPointer<GetMethodDefFromMethodDelegate>(getMethodDefFromMethodPtr);
            ResolveToken = Marshal.GetDelegateForFunctionPointer<ResolveTokenDelegate>(resolveTokenPtr);
        }

        public IntPtr GetMethodClass(IntPtr hMethod)
        {
            return _getMethodClass(_corJitInfo, hMethod);
        }

        public IntPtr GetMethodModule(IntPtr hMethod)
        {
            return _getMethodModule(_corJitInfo, hMethod);
        }

        public uint GetMethodDefFromMethod(IntPtr hMethod)
        {
            return _getMethodDefFromMethod(_corJitInfo, hMethod);
        }
    }
}
