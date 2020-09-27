using Jitex.JIT;

namespace Jitex.Modules
{
    public abstract class JitexBase
    {
        private protected static ManagedJit Jit { get; private set; }

        protected static bool IsInstalled => ManagedJit.IsInstalled;

        protected JitexBase()
        {
            Jit = ManagedJit.GetInstance();
        }

        protected virtual void Load()
        {
            Jit = ManagedJit.GetInstance();
        }

        private protected static void AddCompileResolver(JitexHandler.CompileResolverHandler compileResolverHandler)
        {
            Jit.AddCompileResolver(compileResolverHandler);
        }
        
        private protected static void AddTokenResolver(JitexHandler.TokenResolverHandler tokenResolverHandler)
        {
            Jit.AddTokenResolver(tokenResolverHandler);
        }
        
        private protected static void RemoveCompileResolver(JitexHandler.CompileResolverHandler compileResolverHandler)
        {
            Jit.RemoveCompileResolver(compileResolverHandler);
        }
        
        private protected static void RemoveTokenResolver(JitexHandler.TokenResolverHandler tokenResolverHandler)
        {
            Jit.RemoveTokenResolver(tokenResolverHandler);
        }

        public void Remove()
        {
            Jit.Dispose();
        }
    }
}