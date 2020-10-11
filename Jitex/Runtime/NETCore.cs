using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Jitex.Runtime
{
    internal sealed class NETCore : RuntimeFramework
    {
        [DllImport("clrjit.dll", CallingConvention = CallingConvention.StdCall, SetLastError = true, EntryPoint = "getJit", BestFitMapping = true)]
        private static extern IntPtr GetJit();

        protected override IntPtr GetJitAddress()
        {
            return GetJit();
        }
    }
}
