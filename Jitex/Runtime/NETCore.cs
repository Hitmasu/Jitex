using System;
using System.Runtime.InteropServices;

namespace Jitex.Runtime
{
    internal sealed class NETCore : RuntimeFramework
    {
#if Windows
        private const string jitLibraryName = "clrjit.dll";
#elif Linux
        private const string jitLibraryName = "libclrjit.so";
#else
        private const string jitLibraryName = "libclrjit.dylib";
#endif
        [DllImport(jitLibraryName, CallingConvention = CallingConvention.StdCall, SetLastError = true, EntryPoint = "getJit", BestFitMapping = true)]
        private static extern IntPtr GetJit();

        internal override int ResolveTokenOffset { get; }
        internal override int GetMethodDefFromMethodOffset { get; set; }
        internal override int ConstructStringLiteralOffset { get; set; }

        public NETCore()
        {
            Version runningVersion = GetFrameworkVersion();
            if (runningVersion >= new Version(3, 1, 1))
            {
                ResolveTokenOffset = 0x1C;
                GetMethodDefFromMethodOffset = 0x74;
                ConstructStringLiteralOffset = 0x97;
            }
            else
            {
                ResolveTokenOffset = 0x1A;
                GetMethodDefFromMethodOffset = 0x69;
                ConstructStringLiteralOffset = 0x8B;
            }
        }

        protected override IntPtr GetJitAddress()
        {
            return GetJit();
        }
    }
}
