using System;
using System.Runtime.InteropServices;

namespace Jitex.JIT.CorInfo
{
    internal struct CorJitCompiler
    {
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate CorJitResult CompileMethodDelegate(IntPtr thisPtr, IntPtr comp, ref CORINFO_METHOD_INFO info, uint flags, out IntPtr nativeEntry, out int nativeSizeOfCode);

        public CompileMethodDelegate CompileMethod;
    }
}