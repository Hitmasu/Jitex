using Jitex.JIT;

namespace Jitex
{
    /// <summary>
    /// Jitex manager.
    /// </summary>
    public static class JitexManager
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
        /// <param name="methodResolver">Method resolver to add.</param>
        public static void AddMethodResolver(JitexHandler.MethodResolverHandler methodResolver)
        {
            Jit.AddMethodResolver(methodResolver);
        }

        /// <summary>
        /// Add a token resolver.
        /// </summary>
        /// <param name="tokenResolver">Token resolver to add.</param>
        public static void AddTokenResolver(JitexHandler.TokenResolverHandler tokenResolver)
        {
            Jit.AddTokenResolver(tokenResolver);
        }

        /// <summary>
        /// Remove a method resolver.
        /// </summary>
        /// <param name="methodResolver">Method resolver to remove.</param>
        public static void RemoveMethodResolver(JitexHandler.MethodResolverHandler methodResolver)
        {
            Jit.RemoveMethodResolver(methodResolver);
        }

        /// <summary>
        /// Remove a token resolver.
        /// </summary>
        /// <param name="tokenResolver">Token resolver to remove.</param>
        public static void RemoveTokenResolver(JitexHandler.TokenResolverHandler tokenResolver)
        {
            Jit.RemoveTokenResolver(tokenResolver);
        }

        /// <summary>
        /// If a method resolver is already loaded.
        /// </summary>
        /// <param name="methodResolver">Method resolver.</param>
        /// <returns>True to already loaded. False to not loaded.</returns>
        public static bool HasMethodResolver(JitexHandler.MethodResolverHandler methodResolver) => _jit.HasMethodResolver(methodResolver);

        /// <summary>
        /// If a token resolver is already loaded.
        /// </summary>
        /// <param name="tokenResolver">Token resolver.</param>
        /// <returns>True to already loaded. False to not loaded.</returns>
        public static bool HasTokenResolver(JitexHandler.TokenResolverHandler tokenResolver) => _jit.HasTokenResolver(tokenResolver);

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