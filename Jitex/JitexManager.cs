using System;
using System.Collections.Generic;
using Jitex.JIT;
using Jitex.Utils.Comparer;
using Jitex.Intercept;
using Jitex.JIT.CorInfo;

namespace Jitex
{
    /// <summary>
    /// Jitex manager.
    /// </summary>
    public static class JitexManager
    {
        private static readonly object LockModules = new object();
        private static readonly object MethodResolverLock = new object();
        private static readonly object TokenResolverLock = new object();
        private static readonly object CallInterceptorLock = new object();

        private static ManagedJit? _jit;
        private static InterceptManager? _interceptManager;

        private static ManagedJit Jit => _jit ??= ManagedJit.GetInstance();
        private static InterceptManager InterceptManager => _interceptManager ??= InterceptManager.GetInstance();

        /// <summary>
        /// All modules load on Jitex.
        /// </summary>
        public static IDictionary<Type, JitexModule> ModulesLoaded { get; } = new Dictionary<Type, JitexModule>(TypeEqualityComparer.Instance);

        /// <summary>
        /// Returns if Jitex is loaded on application. 
        /// </summary>
        public static bool IsLoaded => _jit != null && _jit.IsLoaded;

        /// <summary>
        /// Load module on Jitex.
        /// </summary>
        /// <param name="typeModule">Module to load.</param>
        public static void LoadModule(Type typeModule)
        {
            lock (LockModules)
            {
                if (!ModuleIsLoaded(typeModule))
                {
                    JitexModule module = (JitexModule)Activator.CreateInstance(typeModule);

                    module.LoadResolvers();

                    ModulesLoaded.Add(typeModule, module);
                }
            }
        }

        /// <summary>
        /// Load module on Jitex.
        /// </summary>
        /// <param name="typeModule">Module to load.</param>
        public static void LoadModule(Type typeModule, object? instance)
        {
            if (instance == null) throw new ArgumentNullException(nameof(instance));

            lock (LockModules)
            {
                if (!ModuleIsLoaded(typeModule))
                {
                    JitexModule module = (JitexModule)instance;

                    module.LoadResolvers();

                    ModulesLoaded.Add(typeModule, module);
                }
            }
        }

        /// <summary>
        /// Load module on Jitex.
        /// </summary>
        /// <typeparam name="TModule">Module to load.</typeparam>
        public static void LoadModule<TModule>() where TModule : JitexModule, new()
        {
            LoadModule(typeof(TModule));
        }

        /// <summary>
        /// Load module on Jitex.
        /// </summary>
        /// <typeparam name="TModule">Module to load.</typeparam>
        public static void LoadModule<TModule>(TModule instance) where TModule : JitexModule
        {
            LoadModule(typeof(TModule), instance);
        }

        /// <summary>
        /// Remove module from Jitex.
        /// </summary>
        /// <typeparam name="TModule">Module to remove.</typeparam>
        public static void RemoveModule<TModule>() where TModule : JitexModule
        {
            RemoveModule(typeof(TModule));
        }

        /// <summary>
        /// Remove module from Jitex.
        /// </summary>
        /// <param name="typeModule">Module to remove.</param>
        public static void RemoveModule(Type typeModule)
        {
            lock (LockModules)
            {
                if (ModulesLoaded.TryGetValue(typeModule, out JitexModule module))
                {
                    ModulesLoaded.Remove(typeModule);
                    module.Dispose();
                }
            }
        }

        /// <summary>
        /// Returns if module is loaded on Jitex.
        /// </summary>
        /// <typeparam name="TModule">Module to check.</typeparam>
        public static bool ModuleIsLoaded<TModule>() where TModule : JitexModule
        {
            return ModuleIsLoaded(typeof(TModule));
        }

        /// <summary>
        /// Returns if module is loaded on Jitex.
        /// </summary>
        /// <param name="typeModule">Module to check.</param>
        /// <returns></returns>
        public static bool ModuleIsLoaded(Type typeModule)
        {
            return ModulesLoaded.TryGetValue(typeModule, out JitexModule module) && module.IsLoaded;
        }

        public static void AddInterceptor(InterceptHandler.InterceptorHandler interceptorCall)
        {
            InterceptManager.AddCallInterceptor(interceptorCall);
        }

        public static void RemoveInterceptor(InterceptHandler.InterceptorHandler interceptorCall)
        {
            InterceptManager.RemoveCallInterceptor(interceptorCall);
        }

        public static bool HasInterceptor(InterceptHandler.InterceptorHandler interceptorCall)
        {
            return InterceptManager.HasCallInteceptor(interceptorCall);
        }

        public static void EnableIntercept(System.Reflection.MethodBase method)
        {
            InterceptManager.EnableIntercept(method);
        }

        public static void DisableIntercept(System.Reflection.MethodBase method)
        {
            InterceptManager.RemoveIntercept(method);
        }

        /// <summary>
        /// Add a method resolver.
        /// </summary>
        /// <param name="methodResolver">Method resolver to add.</param>
        public static void AddMethodResolver(JitexHandler.MethodResolverHandler methodResolver)
        {
            lock (MethodResolverLock)
                Jit.AddMethodResolver(methodResolver);
        }

        /// <summary>
        /// Add a token resolver.
        /// </summary>
        /// <param name="tokenResolver">Token resolver to add.</param>
        public static void AddTokenResolver(JitexHandler.TokenResolverHandler tokenResolver)
        {
            lock (TokenResolverLock)
                Jit.AddTokenResolver(tokenResolver);
        }

        /// <summary>
        /// Remove a method resolver.
        /// </summary>
        /// <param name="methodResolver">Method resolver to remove.</param>
        public static void RemoveMethodResolver(JitexHandler.MethodResolverHandler methodResolver)
        {
            lock (MethodResolverLock)
                Jit.RemoveMethodResolver(methodResolver);
        }

        /// <summary>
        /// Remove a token resolver.
        /// </summary>
        /// <param name="tokenResolver">Token resolver to remove.</param>
        public static void RemoveTokenResolver(JitexHandler.TokenResolverHandler tokenResolver)
        {
            lock (TokenResolverLock)
                Jit.RemoveTokenResolver(tokenResolver);
        }

        /// <summary>
        /// Returns if a method resolver is already loaded.
        /// </summary>
        /// <param name="methodResolver">Method resolver.</param>
        /// <returns>True to already loaded. False to not loaded.</returns>
        public static bool HasMethodResolver(JitexHandler.MethodResolverHandler methodResolver) => Jit.HasMethodResolver(methodResolver);

        /// <summary>
        /// Returns If a token resolver is already loaded.
        /// </summary>
        /// <param name="tokenResolver">Token resolver.</param>
        /// <returns>True to already loaded. False to not loaded.</returns>
        public static bool HasTokenResolver(JitexHandler.TokenResolverHandler tokenResolver) => Jit.HasTokenResolver(tokenResolver);

        /// <summary>
        /// Unload Jitex and modules from application.
        /// </summary>
        public static void Remove()
        {
            if (_jit != null)
            {
                ModulesLoaded.Clear();

                _jit.Dispose();
                _jit = null;
            }
        }
    }
}