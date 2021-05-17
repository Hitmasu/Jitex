using System;
using System.Reflection;

namespace Jitex.JIT.Context
{
    internal class InterceptContext : DetourContext
    {
        public MethodBase MethodIntercepted { get; }

        public IntPtr MethodOriginalAddress { get; set; }

        public IntPtr MethodTrampolineAddress
        {
            set => MethodAddress = value;
        }
        
        public InterceptContext(MethodBase methodIntercepted, MethodBase methodInterceptor) : base(methodInterceptor)
        {
            MethodIntercepted = methodIntercepted;
        }
    }
}