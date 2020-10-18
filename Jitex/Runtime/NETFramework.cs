using System;
using System.Runtime.InteropServices;

namespace Jitex.Runtime
{
    internal sealed class NETFramework : RuntimeFramework
    {
        [DllImport("mscorjit.dll", CallingConvention = CallingConvention.StdCall, SetLastError = true, EntryPoint = "getJit", BestFitMapping = true)]
        private static extern IntPtr GetJit();

        public NETFramework() : base(false)
        {
        }

        protected override IntPtr GetJitAddress()
        {
            return GetJit();
        }
    }
}
