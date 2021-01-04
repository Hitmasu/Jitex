using System;
using System.Reflection;

namespace Jitex.JIT.Context
{
    internal class InterceptContext : DetourContext
    {
        public MethodBase Method { get; }

        public IntPtr PrimaryNativeAddress { get; set; }

        public IntPtr SecondaryNativeAddress
        {
            get => NativeAddress;
            set => NativeAddress = value;
        }
        
        public InterceptContext(MethodBase method, byte[] nativeCode) : base(nativeCode)
        {
            Method = method;
        }
    }
}