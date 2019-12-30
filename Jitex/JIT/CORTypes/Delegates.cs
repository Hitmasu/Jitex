using System;
using System.Runtime.InteropServices;
using static Jitex.JIT.PInvoke.Structs;

namespace Jitex.JIT.PInvoke
{
    internal static class Delegates
    {
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate IntPtr GetJitDelegate();

        /// <summary>
        /// Wrap delegate to compileMethod from ICorJitCompiler.
        /// <see cref="https://github.com/dotnet/coreclr/blob/c51aa9006c035ccdf8aab2e9a363637e8c6e31da/src/inc/corjit.h#L405"/>
        /// </summary>
        /// <param name="thisPtr">this parameter.</param>
        /// <param name="comp">(IN) - Pointer to ICorJitInfo.</param>
        /// <param name="info">(IN) - Pointer to CORINFO_METHOD_INFO.</param>
        /// <param name="flags">(IN) - Pointer to CorJitFlag.</param>
        /// <param name="nativeEntry">(OUT) - Pointer to NativeEntry.</param>
        /// <param name="nativeSizeOfCode">(OUT) - Size of NativeEntry.</param>
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int CompileMethodDelegate(IntPtr thisPtr, IntPtr comp, ref CORINFO_METHOD_INFO info, uint flags, out IntPtr nativeEntry, out int nativeSizeOfCode);

        /// <summary>
        /// Wrap delegate to getMethodDefFromMethodDelegate from ICorJitInfo.
        /// <see cref="https://github.com/dotnet/coreclr/blob/c51aa9006c035ccdf8aab2e9a363637e8c6e31da/src/inc/corinfo.h#L2762"/>
        /// </summary>
        /// <param name="thisPtr">this parameter.</param>
        /// <param name="hMethodHandle">(IN) - Pointer to method handler</param>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int GetMethodDefFromMethodDelegate(IntPtr thisPtr, IntPtr hMethodHandle);
    }
}