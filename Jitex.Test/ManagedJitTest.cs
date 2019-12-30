using System;
using System.Runtime.CompilerServices;
using Jitex.JIT;
using Xunit;

namespace Jitex.Test
{
    public class ManagedJitTest
    {
        private readonly ManagedJit _jit;

        public ManagedJitTest()
        {
            _jit = ManagedJit.GetInstance();
        }

        [Fact]
        public void MethodBodyReturn()
        {
            
        }
    }

    public class ManagedJitTestMethods
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public string ReturnText(string text) => text;

        public string ModifiedText(string text) => new Guid().ToString("X");
    }
}
