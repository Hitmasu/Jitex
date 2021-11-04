using System;
using System.Collections.Generic;
using Jitex.JIT;
using Jitex.Utils.Comparer;
using Jitex.Intercept;
using static Jitex.JIT.JitexHandler;

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
        private static readonly object OnMethodCompiledLock = new object();

        private static ManagedJit? _jit;
        private static InterceptManager? _interceptManager;

        private static ManagedJit Jit => _jit ??= ManagedJit.GetInstance();
        private static InterceptManager InterceptManager => _interceptManager ??= InterceptManager.GetInstance();

        /// <summary>
        /// All modules load on Jitex.
        /// </summary>
        private static IDictionary<Type, JitexModule> ModulesLoaded { get; } = new Dictionary<Type, JitexModule>(TypeEqualityComparer.Instance);

        /// <summary>
        /// Event to raise when method was compiled.
        /// </summary>
        public static event MethodCompiledHandler OnMethodCompiled
        {
            add => AddOnMethodCompiled(value);
            remove => RemoveOnMethodCompiled(value);
        }

        /// <summary>
        /// Method resolver.
        /// </summary>
        public static event MethodResolverHandler MethodResolver
        {
            add => AddMethodResolver(value);
            remove => RemoveMethodResolver(value);
        }


        /// <summary>
        /// Token resolver.
        /// </summary>
        public static event TokenResolverHandler TokenResolver
        {
            add => AddTokenResolver(value);
            remove => RemoveTokenResolver(value);
        }

        /// <summary>
        /// Call interceptor
        /// </summary>
        public static event InterceptHandler.InterceptorHandler Interceptor
        {
            add => AddInterceptor(value);
            remove => RemoveInterceptor(value);
        }

        /// <summary>
        /// Returns if Jitex is enabled. 
        /// </summary>
        public static bool IsEnabled => _jit is {IsEnabled: true};

        /// <summary>
        /// Enable Jitex
        /// </summary>
        public static void EnableJitex() => Jit.Enable();

        /// <summary>
        /// Disable Jitex
        /// </summary>
        public static void DisableJitex() => Jit.Disable();

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
                    JitexModule module = (JitexModule) Activator.CreateInstance(typeModule);

                    module.LoadResolvers();

                    ModulesLoaded.Add(typeModule, module);
                }
            }
        }

        /// <summary>
        /// Load module on Jitex.
        /// </summary>
        /// <param name="typeModule">Module to load.</param>
        /// <param name="instance">Instance of type.</param>
        public static void LoadModule(Type typeModule, object? instance)
        {
            if (instance == null) throw new ArgumentNullException(nameof(instance));

            lock (LockModules)
            {
                if (!ModuleIsLoaded(typeModule))
                {
                    JitexModule module = (JitexModule) instance;

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

        /// <summary>
        /// Add a new interceptor.
        /// </summary>
        /// <param name="interceptorCallAsync">Interceptor to call.</param>
        public static void AddInterceptor(InterceptHandler.InterceptorHandler interceptorCallAsync)
        {
            lock (CallInterceptorLock)
            {
                InterceptManager.AddInterceptorCall(interceptorCallAsync);

                if (!IsEnabled)
                    EnableJitex();
            }
        }

        /// <summary>
        /// Remove a interceptor.
        /// </summary>
        /// <param name="interceptorCall">Interceptor to remove.</param>
        public static void RemoveInterceptor(InterceptHandler.InterceptorHandler interceptorCall)
        {
            lock (CallInterceptorLock)
                InterceptManager.RemoveInterceptorCall(interceptorCall);
        }

        /// <summary>
        /// Check if interceptor is loaded.
        /// </summary>
        /// <param name="interceptorCall">Intercept to check.</param>
        /// <returns>Returns true if loaded, otherwise returns false.</returns>
        public static bool HasInterceptor(InterceptHandler.InterceptorHandler interceptorCall)
        {
            lock (CallInterceptorLock)
                return InterceptManager.HasInteceptorCall(interceptorCall);
        }

        /// <summary>
        /// Enable intercept call on method (Only if intercept was disabled).
        /// </summary>
        /// <param name="method">Method to enable intercept call.</param>
        public static void EnableIntercept(System.Reflection.MethodBase method)
        {
            lock (CallInterceptorLock)
                InterceptManager.EnableIntercept(method);
        }

        /// <summary>
        /// Disable intercept call on method.
        /// </summary>
        /// <param name="method">Method to disable intercept call.</param>
        public static void DisableIntercept(System.Reflection.MethodBase method)
        {
            lock (CallInterceptorLock)
                InterceptManager.RemoveIntercept(method);
        }

        /// <summary>
        /// Add a method resolver.
        /// </summary>
        /// <param name="methodResolver">Method resolver to add.</param>
        public static void AddMethodResolver(JitexHandler.MethodResolverHandler methodResolver)
        {
            lock (MethodResolverLock)
            {
                Jit.AddMethodResolver(methodResolver);

                if (!IsEnabled)
                    EnableJitex();
            }
        }

        /// <summary>
        /// Add a token resolver.
        /// </summary>
        /// <param name="tokenResolver">Token resolver to add.</param>
        public static void AddTokenResolver(JitexHandler.TokenResolverHandler tokenResolver)
        {
            lock (TokenResolverLock)
            {
                Jit.AddTokenResolver(tokenResolver);

                if (!IsEnabled)
                    EnableJitex();
            }
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
        /// Added event to raise after method was compiled
        /// </summary>
        /// <param name="onMethodCompiled"></param>
        public static void AddOnMethodCompiled(MethodCompiledHandler onMethodCompiled)
        {
            lock (OnMethodCompiledLock)
                Jit.AddOnMethodCompiledEvent(onMethodCompiled);
        }

        /// <summary>
        /// Remove event after method was compiled.
        /// </summary>
        /// <param name="onMethodCompiled"></param>
        public static void RemoveOnMethodCompiled(MethodCompiledHandler onMethodCompiled)
        {
            lock (OnMethodCompiledLock)
                Jit.RemoveOnMethodCompiledEvent(onMethodCompiled);
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