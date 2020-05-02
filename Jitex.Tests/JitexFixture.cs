using System;
using System.Diagnostics;
using Jitex.JIT;

namespace Jitex.Tests
{
    public class JitexFixture : IDisposable
    {
        private readonly ManagedJit _jit;

        public ManagedJit.PreCompileHandle OnPreCompile
        {
            get => _jit.OnPreCompile;
            set => _jit.OnPreCompile = value;
        }

        public ManagedJit.ResolveTokenHandle OnResolveToken
        {
            get => _jit.OnResolveToken;
            set => _jit.OnResolveToken = value;
        }

        public JitexFixture()
        {
            _jit = ManagedJit.GetInstance();
        }

        public void Dispose()
        { 
            _jit.Dispose();
        }
    }
}
