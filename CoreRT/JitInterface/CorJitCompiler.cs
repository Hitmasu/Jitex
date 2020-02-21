using System;
using System.Runtime.InteropServices;

namespace CoreRT.JitInterface
{
    public struct CorJitCompiler
    {
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int CompileMethodDelegate(IntPtr thisPtr, IntPtr comp, ref CORINFO_METHOD_INFO info, uint flags, out IntPtr nativeEntry, out int nativeSizeOfCode);

        public CompileMethodDelegate CompileMethod;
    }
}
