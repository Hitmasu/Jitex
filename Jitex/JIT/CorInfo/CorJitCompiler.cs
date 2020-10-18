using System;
using System.Runtime.InteropServices;

namespace Jitex.JIT.CorInfo
{
    internal struct CorJitCompiler
    {
        /// <summary>
        /// Compile method handler.
        /// </summary>
        /// <param name="thisPtr">this parameter</param>
        /// <param name="comp">(IN) - Pointer to ICorJitInfo.</param>
        /// <param name="info">(IN) - Pointer to CORINFO_METHOD_INFO.</param>
        /// <param name="flags">(IN) - Pointer to CorJitFlag.</param>
        /// <param name="nativeEntry">(OUT) - Pointer to NativeEntry.</param>
        /// <param name="nativeSizeOfCode">(OUT) - Size of NativeEntry.</param>
        /// <returns></returns>
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate CorJitResult CompileMethodDelegate(IntPtr thisPtr, IntPtr comp, IntPtr info, uint flags, out IntPtr nativeEntry, out int nativeSizeOfCode);

        /// <summary>
        /// Compile method delegate.
        /// </summary>
        public CompileMethodDelegate CompileMethod;
    }
}