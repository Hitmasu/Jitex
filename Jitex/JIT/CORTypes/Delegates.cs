using System;
using System.Runtime.InteropServices;
using static Jitex.JIT.CORTypes.Structs;

namespace Jitex.JIT.CORTypes
{
    internal static class Delegates
    {
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate IntPtr GetJitDelegate();

        /// <summary>
        /// Wrap delegate to compileMethod from ICorJitCompiler.
        /// <see cref="https://github.com/dotnet/runtime/blob/f8eabc47a04a25e3cfa4afc78161e0d47209eb57/src/coreclr/src/inc/corjit.h#L238"/>
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
        /// <see cref="https://github.com/dotnet/runtime/blob/f8eabc47a04a25e3cfa4afc78161e0d47209eb57/src/coreclr/src/inc/corinfo.h#L2910"/>
        /// </summary>
        /// <param name="thisPtr">this parameter.</param>
        /// <param name="hMethodHandle">(IN) - Pointer to method handle.</param>
        /// <return>Method token.</return>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int GetMethodDefFromMethodDelegate(IntPtr thisPtr, IntPtr hMethodHandle);


        /// <summary>
        /// Wrap delegate to getModuleAssembly from ICorJitInfo.
        /// <see cref="https://github.com/dotnet/runtime/blob/f8eabc47a04a25e3cfa4afc78161e0d47209eb57/src/coreclr/src/inc/corinfo.h#L2391"/>
        /// </summary>
        /// <param name="thisPtr">this parameter.</param>
        /// <param name="moduleHandle">(IN) - Pointer to module handle.</param>
        /// <returns>Handle from assembly.</returns>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr GetModuleAssemblyDelegate(IntPtr thisPtr, IntPtr moduleHandle);

        /// <summary>
        /// Wrap delegate to getNameAssembly from ICorJitInfo.
        /// <see cref="https://github.com/dotnet/runtime/blob/f8eabc47a04a25e3cfa4afc78161e0d47209eb57/src/coreclr/src/inc/corinfo.h#L2396"/>
        /// </summary>
        /// <param name="thisPtr">this parameter.</param>
        /// <param name="assemblyHandle">(IN) - Pointer to assembly handle.</param>
        /// <returns>Handle from assembly.</returns>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr GetAssemblyName(IntPtr thisPtr, IntPtr assemblyHandle);
    }
}