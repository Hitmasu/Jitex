using System;
using System.Diagnostics;
using System.Reflection;

namespace Jitex.JIT
{
    [DebuggerDisplay("{Method.Name}")]

    public class MethodCompiled
    {
        public IntPtr CILJit { get; set; }
        public MethodBase Method { get; }
        public IntPtr NativeCodeAddress { get; }
        public int NativeCodeSize { get; }
        public IntPtr Handle { get; }
        public IntPtr Comp { get; }
        public uint Flags { get; }

        public MethodCompiled(MethodBase method, IntPtr cilJit, IntPtr comp, IntPtr handle, uint flags, IntPtr nativeCodeAddress, int nativeCodeSize)
        {
            Method = method;
            NativeCodeAddress = nativeCodeAddress;
            NativeCodeSize = nativeCodeSize;
            Handle = handle;
            Flags = flags;
            Comp = comp;
            CILJit = cilJit;
        }
    }
}