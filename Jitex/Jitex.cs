using Jitex.JIT;

namespace Jitex
{
    public static class Jitex
    {
        private static ManagedJit _jit;

        private static ManagedJit Jit => _jit ??= ManagedJit.GetInstance();

        public static bool IsLoaded => ManagedJit.IsLoaded;

        public static void AddCompileResolver(JitexHandler.CompileResolverHandler compileResolverHandler)
        {
            Jit.AddCompileResolver(compileResolverHandler);
        }

        public static void AddTokenResolver(JitexHandler.TokenResolverHandler tokenResolverHandler)
        {
            Jit.AddTokenResolver(tokenResolverHandler);
        }

        public static void RemoveCompileResolver(JitexHandler.CompileResolverHandler compileResolverHandler)
        {
            Jit.RemoveCompileResolver(compileResolverHandler);
        }

        public static void RemoveTokenResolver(JitexHandler.TokenResolverHandler tokenResolverHandler)
        {
            Jit.RemoveTokenResolver(tokenResolverHandler);
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