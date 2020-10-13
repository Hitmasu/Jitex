using System;
using System.Runtime.InteropServices;
using Jitex.JIT.CorInfo;
using Jitex.Utils;

namespace Jitex.Runtime
{
    internal abstract class RuntimeFramework
    {
        /// <summary>
        /// Address of JIT.
        /// </summary>
        public IntPtr Jit { get; }

        /// <summary>
        /// Address of table JIT.
        /// </summary>
        public IntPtr JitVTable { get; }

        /// <summary>
        /// Address of ICorJitInfo.
        /// </summary>
        public IntPtr CorJitInfo { get; set; }

        /// <summary>
        /// CEEInfo instance.
        /// </summary>
        public CEEInfo CEEInfo { get; set; }

        /// <summary>
        /// Compile method
        /// </summary>
        /// <remarks>
        /// Get address of compile method.
        /// No get; implementation bacause of copy-safe.
        /// </remarks>
        public CorJitCompiler CorJitCompiler;

        /// <summary>
        /// Runtime running.
        /// </summary>
        private static RuntimeFramework Framework { get; set; }

        /// <summary>
        /// Load info from JIT.
        /// </summary>
        protected RuntimeFramework()
        {
            Jit = GetJitAddress();
            JitVTable = GetJitVTableAddress();
            CorJitCompiler = GetCompileMethod();
        }

        /// <summary>
        /// Get running framework.
        /// </summary>
        /// <returns></returns>
        public static RuntimeFramework GetFramework()
        {
            if (Framework != null)
                return Framework;

            string frameworkRunning = RuntimeInformation.FrameworkDescription;

            if (frameworkRunning.StartsWith(".NET Core"))
                Framework = new NETCore();
            else if (frameworkRunning.StartsWith(".NET Framework"))
                Framework = new NETFramework();
            else
                throw new NotSupportedException($"Framework {frameworkRunning} is not supported!");

            return Framework;
        }

        /// <summary>
        /// Get address of method getJit.
        /// </summary>
        /// <returns></returns>
        protected abstract IntPtr GetJitAddress();

        /// <summary>
        /// Get address of table from JIT.
        /// </summary>
        /// <returns></returns>
        protected IntPtr GetJitVTableAddress()
        {
            return Marshal.ReadIntPtr(Jit);
        }

        /// <summary>
        /// Get struct from ICorJitCompiler
        /// </summary>
        /// <returns></returns>
        protected CorJitCompiler GetCompileMethod()
        {
            return Marshal.PtrToStructure<CorJitCompiler>(JitVTable);
        }

        /// <summary>
        /// Read CORJitInfo.
        /// </summary>
        /// <param name="iCorJitInfo">Address to ICorJitInfo.</param>
        public void ReadJITInfo(IntPtr iCorJitInfo)
        {
            CorJitInfo = Marshal.ReadIntPtr(iCorJitInfo);
            CEEInfo = new CEEInfo(CorJitInfo);
        }
    }
}
