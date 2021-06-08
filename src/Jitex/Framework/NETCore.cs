using System;
using System.Runtime.InteropServices;

namespace Jitex.Framework
{
    internal sealed class NETCore : RuntimeFramework
    {
        public NETCore() : base(true)
        {
        }

        protected override IntPtr GetJitAddress()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return OSXLibrary.GetJit();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return LinuxLibrary.GetJit();
            
            return WindowsLibrary.GetJit();
        }


        private static class OSXLibrary
        {
            [DllImport("libclrjit.dylib", CallingConvention = CallingConvention.StdCall, SetLastError = true, EntryPoint = "getJit", BestFitMapping = true)]
            public static extern IntPtr GetJit();
        }

        private static class LinuxLibrary
        {
            [DllImport("libclrjit.so", CallingConvention = CallingConvention.StdCall, SetLastError = true, EntryPoint = "getJit", BestFitMapping = true)]
            public static extern IntPtr GetJit();
        }

        private static class WindowsLibrary
        {
            [DllImport("clrjit.dll", CallingConvention = CallingConvention.StdCall, SetLastError = true, EntryPoint = "getJit", BestFitMapping = true)]
            public static extern IntPtr GetJit();
        }
    }
}