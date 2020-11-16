using System;
using System.Runtime.InteropServices;
using NativeLibraryLoader;

namespace Jitex.Runtime
{
    internal sealed class NETCore : RuntimeFramework
    {
        private delegate IntPtr GetJitDelegate();

        public NETCore() : base(true)
        {
        }

        protected override IntPtr GetJitAddress()
        {
            string jitLibraryName = string.Empty;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                jitLibraryName = "clrjit.dll";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                jitLibraryName = "libclrjit.so";
            }
            else
            {
                jitLibraryName = "libclrjit.dylib";
            }

            LibraryLoader? defaultLoader = LibraryLoader.GetPlatformDefaultLoader();

            IntPtr libAddress = defaultLoader.LoadNativeLibrary(jitLibraryName);
            IntPtr getJitAddress = defaultLoader.LoadFunctionPointer(libAddress, "getJit");

            GetJitDelegate getJit = Marshal.GetDelegateForFunctionPointer<GetJitDelegate>(getJitAddress);
            IntPtr jitAddress = getJit();
            return jitAddress;
        }
    }
}