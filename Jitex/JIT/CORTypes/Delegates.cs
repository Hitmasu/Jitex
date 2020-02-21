using System;
using System.Runtime.InteropServices;
using CoreRT.JitInterface;

namespace Jitex.JIT.CORTypes
{
    internal static class Delegates
    {

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


        /// <summary>
        /// Wrap delegate to getNameAssembly from ICorJitInfo.
        /// <see cref="https://github.com/dotnet/runtime/blob/f8eabc47a04a25e3cfa4afc78161e0d47209eb57/src/coreclr/src/inc/corinfo.h#L2396"/>
        /// </summary>
        /// <param name="thisPtr">this parameter.</param>
        /// <param name="assemblyHandle">(IN) - Pointer to assembly handle.</param>
        /// <returns>Handle from assembly.</returns>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr GetAssemblyName(IntPtr thisPtr, IntPtr assemblyHandle);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate bool IsJitOptimizationDisabled(IntPtr thisPtr);
    }
}