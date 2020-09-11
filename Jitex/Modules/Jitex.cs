using ResolveCompileHandle = Jitex.JIT.JitexHandler.ResolveCompileHandle;
using ResolveTokenHandle = Jitex.JIT.JitexHandler.ResolveTokenHandle;

namespace Jitex.JIT
{
    public static class Jitex
    {
        private static readonly ManagedJit Instance;

        static Jitex()
        {
            Instance = ManagedJit.GetInstance();
        }
        
        public static void AddCompileResolver(ResolveCompileHandle compileResolver)
        {
            Instance.AddCompileResolver(compileResolver);
        }

        public static void AddTokenResolver(ResolveTokenHandle tokenResolver)
        {
            Instance.AddTokenResolver(tokenResolver);
        }
        
        public static void RemoveCompileResolver(ResolveCompileHandle compileResolver)
        {
            Instance.RemoveCompileResolver(compileResolver);
        }

        public static void RemoveTokenResolver(ResolveTokenHandle tokenResolver)
        {
            Instance.RemoveTokenResolver(tokenResolver);
        }
    }
}