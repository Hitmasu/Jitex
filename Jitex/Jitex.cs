using Jitex.JIT;

namespace Jitex
{
    /// <summary>
    /// Jitex manager.
    /// </summary>
    public static class Jitex
    {
        private static ManagedJit _jit;

        private static ManagedJit Jit => _jit ??= ManagedJit.GetInstance();

        /// <summary>
        /// Returns if Jitex is loaded on application. 
        /// </summary>
        public static bool IsLoaded => ManagedJit.IsLoaded;

        /// <summary>
        /// Add a method resolver.
        /// </summary>
        /// <param name="methodResolverHandler">Method resolver to add.</param>
        public static void AddMethodResolver(JitexHandler.MethodResolverHandler methodResolverHandler)
        {
            Jit.AddMethodResolver(methodResolverHandler);
        }

        /// <summary>
        /// Add a token resolver.
        /// </summary>
        /// <param name="tokenResolverHandler">Token resolver to add.</param>
        public static void AddTokenResolver(JitexHandler.TokenResolverHandler tokenResolverHandler)
        {
            Jit.AddTokenResolver(tokenResolverHandler);
        }

        /// <summary>
        /// Remove a method resolver.
        /// </summary>
        /// <param name="methodResolverHandler">Method resolver to remove.</param>
        public static void RemoveMethodResolver(JitexHandler.MethodResolverHandler methodResolverHandler)
        {
            Jit.RemoveMethodResolver(methodResolverHandler);
        }

        /// <summary>
        /// Remove a token resolver.
        /// </summary>
        /// <param name="tokenResolverHandler">Token resolver to remove.</param>
        public static void RemoveTokenResolver(JitexHandler.TokenResolverHandler tokenResolverHandler)
        {
            Jit.RemoveTokenResolver(tokenResolverHandler);
        }

        /// <summary>
        /// If a method resolver is already loaded.
        /// </summary>
        /// <param name="methodResolverHandler">Method resolver.</param>
        /// <returns>True to already loaded. False to not loaded.</returns>
        public static bool HasMethodResolver(JitexHandler.MethodResolverHandler methodResolverHandler) => _jit.HasMethodResolver(methodResolverHandler);

        /// <summary>
        /// If a token resolver is already loaded.
        /// </summary>
        /// <param name="tokenResolverHandler">Token resolver.</param>
        /// <returns>True to already loaded. False to not loaded.</returns>
        public static bool HasTokenResolver(JitexHandler.TokenResolverHandler tokenResolverHandler) => _jit.HasTokenResolver(tokenResolverHandler);

        /// <summary>
        /// Unload Jitex and modules from application.
        /// </summary>
        public static void Remove()
        {
            _jit.Dispose();
            _jit = null;
        }
    }
}