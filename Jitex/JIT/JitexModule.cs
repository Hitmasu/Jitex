using System;

namespace Jitex.JIT
{
    public abstract class JitexModule : IDisposable
    {
        private static ManagedJit _jit;

        protected JitexModule()
        {
            _jit ??= ManagedJit.GetInstance();
        }

        public void Initialize()
        {
            _jit.AddCompileResolver(CompileResolver);
            _jit.AddTokenResolver(TokenResolver);
        }

        protected abstract void CompileResolver(CompileContext context);
        
        protected abstract void TokenResolver(TokenContext context);

        protected void RemoveJitex()
        {
            _jit.Dispose();
        }
        
        public void Dispose()
        {
            _jit.RemoveCompileResolver(CompileResolver);
            _jit.RemoveTokenResolver(TokenResolver);
        }
    }
}