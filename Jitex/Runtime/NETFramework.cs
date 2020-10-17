using System;
using System.Runtime.InteropServices;

namespace Jitex.Runtime
{
    internal sealed class NETFramework : RuntimeFramework
    {
        [DllImport("mscorjit.dll", CallingConvention = CallingConvention.StdCall, SetLastError = true, EntryPoint = "getJit", BestFitMapping = true)]
        private static extern IntPtr GetJit();

        internal override int ResolveTokenOffset { get; }
        internal override int GetMethodDefFromMethodOffset { get; set; }
        internal override int ConstructStringLiteralOffset { get; set; }

        protected override IntPtr GetJitAddress()
        {
            return GetJit();
        }
    }
}
