using Jitex.JIT;

namespace Jitex
{
    public static class Jitex
    {
        private static ManagedJit _jit;

        public static bool IsInstalled => ManagedJit.IsInstalled;

        public static void AddCompileResolver(JitexHandler.CompileResolverHandler compileResolverHandler)
        {
            _jit.AddCompileResolver(compileResolverHandler);
        }

        public static void AddTokenResolver(JitexHandler.TokenResolverHandler tokenResolverHandler)
        {
            _jit.AddTokenResolver(tokenResolverHandler);
        }

        public static void RemoveCompileResolver(JitexHandler.CompileResolverHandler compileResolverHandler)
        {
            _jit.RemoveCompileResolver(compileResolverHandler);
        }

        public static void RemoveTokenResolver(JitexHandler.TokenResolverHandler tokenResolverHandler)
        {
            _jit.RemoveTokenResolver(tokenResolverHandler);
        }

        public static bool HasCompileResolver(JitexHandler.CompileResolverHandler compileResolverHandler) => _jit.HasCompileResolver(compileResolverHandler);

        public static bool HasTokenResolver(JitexHandler.TokenResolverHandler tokenResolverHandler) => _jit.HasTokenResolver(tokenResolverHandler);

        public static void Remove()
        {
            _jit.Dispose();
            _jit = null;
        }
    }
}