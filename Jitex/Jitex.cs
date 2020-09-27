using Jitex.JIT;

namespace Jitex
{
    public class Jitex : JitexBase
    {
        public static void AddCompileResolver(JitexHandler.CompileResolverHandler compileResolver)
        {
            Jit.AddCompileResolver(compileResolver);
        }

        public static void AddTokenResolver(JitexHandler.TokenResolverHandler tokenResolver)
        {
            Jit.AddTokenResolver(tokenResolver);
        }

        public static void RemoveCompileResolver(JitexHandler.CompileResolverHandler compileResolver)
        {
            Jit.RemoveCompileResolver(compileResolver);
        }

        public static void RemoveTokenResolver(JitexHandler.TokenResolverHandler tokenResolver)
        {
            Jit.RemoveTokenResolver(tokenResolver);
        }
    }
}