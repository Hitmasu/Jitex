using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Jitex.JIT.CorInfo;

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
        public IntPtr ICorJitCompileVTable { get; }

        /// <summary>
        /// Address of ICorJitInfo.
        /// </summary>
        public IntPtr CEEInfoVTable { get; set; }

        /// <summary>
        /// Offset from ResolveToken in VTable.
        /// </summary>
        internal abstract int ResolveTokenOffset { get; }

        /// <summary>
        /// Offset from GetMethodDefFromMethod in VTable.
        /// </summary>
        internal abstract int GetMethodDefFromMethodOffset { get; set; }

        /// <summary>
        /// Offset from ConstructStringLiteral in VTable.
        /// </summary>
        internal abstract int ConstructStringLiteralOffset { get; set; }

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
            ICorJitCompileVTable = GetJitVTableAddress();
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
            return Marshal.PtrToStructure<CorJitCompiler>(ICorJitCompileVTable);
        }

        /// <summary>
        /// Read CORJitInfo.
        /// </summary>
        /// <param name="iCorJitInfo">Address to ICorJitInfo.</param>
        public void ReadICorJitInfoVTable(IntPtr iCorJitInfo)
        {
            CEEInfoVTable = Marshal.ReadIntPtr(iCorJitInfo);
        }

        protected static Version GetFrameworkVersion()
        {
            Assembly assembly = typeof(System.Runtime.GCSettings).GetTypeInfo().Assembly;
            string[] assemblyPath = assembly.CodeBase.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
            int netCoreAppIndex = Array.IndexOf(assemblyPath, "Microsoft.NETCore.App");
            if (netCoreAppIndex > 0 && netCoreAppIndex < assemblyPath.Length - 2)
            {
                string version = assemblyPath[netCoreAppIndex + 1];
                int[] versionsNumbers = version.Split('.').Select(int.Parse).ToArray();
                return new Version(versionsNumbers[0], versionsNumbers[1], versionsNumbers[2]);
            }
            throw new NotSupportedException("Invalid Framework");
        }
    }
}
