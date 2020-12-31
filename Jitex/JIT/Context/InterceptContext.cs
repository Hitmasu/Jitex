using System;
using System.Reflection;

namespace Jitex.JIT.Context
{
    internal class InterceptContext : DetourContext
    {
        public MethodBase Method { get; }

        public IntPtr PrimaryNativeAddress
        {
            get => NativeAddress;
            set => NativeAddress = value;
        }
        public IntPtr SecondaryNativeAddress { get; set; }

        public InterceptContext(MethodBase method, byte[] nativeCode) : base(nativeCode)
        {
            Method = method;
        }
    }
}