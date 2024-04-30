using System;
using System.Collections.Generic;
using Jitex.Framework;
using Jitex.JIT;
using Jitex.Utils.Comparer;
using Jitex.Intercept;
using Jitex.JIT.Hooks.CompileMethod;
using Jitex.JIT.Hooks.String;
using Jitex.JIT.Hooks.Token;

namespace Jitex
{
    /// <summary>
    /// Jitex manager.
    /// </summary>
    public static class JitexManager
    {
        private static InterceptManager? _interceptManager;
        private static InterceptManager InterceptManager => _interceptManager ??= InterceptManager.GetInstance();
        private static CompileMethodHook CompileMethodHook => CompileMethodHook.GetInstance();
        private static TokenHook TokenHook => TokenHook.GetInstance();
        private static StringHook StringHook => StringHook.GetInstance();

        /// <summary>
        /// All modules load on Jitex.
        /// </summary>
        private static IDictionary<Type, JitexModule> ModulesLoaded { get; } =
            new Dictionary<Type, JitexModule>(TypeEqualityComparer.Instance);

        static JitexManager()
        {
            CompileMethodHook.PrepareHook();
            TokenHook.PrepareHook();
            StringHook.PrepareHook();
        }

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
        /// String resolver
        /// </summary>
        public static event StringResolverHandler StringResolver
        {
            add => AddStringResolver(value);
            remove => RemoveStringResolver(value);
        }

        /// <summary>
        /// Returns if Jitex is enabled. 
        /// </summary>
        public static bool IsEnabled => CompileMethodHook.IsEnabled;

        /// <summary>
        /// Enable Jitex
        /// </summary>
        public static void EnableJitex()
        {
            CompileMethodHook.InjectHook(RuntimeFramework.Framework.ICorJitCompileVTable);
            // TokenHook.InjectHook(RuntimeFramework.Framework.CEEInfoVTable);
            // StringHook.InjectHook(RuntimeFramework.Framework.CEEInfoVTable);
        }

        /// <summary>
        /// Disable Jitex
        /// </summary>
        public static void DisableJitex()
        {
            CompileMethodHook.RemoveHook();
            // TokenHook.RemoveHook();
            // StringHook.RemoveHook();
        }

        /// <summary>
        /// Load module on Jitex.
        /// </summary>
        /// <param name="typeModule">Module to load.</param>
        public static void LoadModule(Type typeModule)
        {
            if (!ModuleIsLoaded(typeModule))
            {
                JitexModule module = (JitexModule)Activator.CreateInstance(typeModule);

                module.LoadResolvers();

                ModulesLoaded.Add(typeModule, module);
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

            if (!ModuleIsLoaded(typeModule))
            {
                JitexModule module = (JitexModule)instance;

                module.LoadResolvers();

                ModulesLoaded.Add(typeModule, module);
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
            if (ModulesLoaded.TryGetValue(typeModule, out JitexModule module))
            {
                ModulesLoaded.Remove(typeModule);
                module.Dispose();
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
            InterceptManager.AddInterceptorCall(interceptorCallAsync);

            if (!IsEnabled)
                EnableJitex();
        }

        /// <summary>
        /// Remove a interceptor.
        /// </summary>
        /// <param name="interceptorCall">Interceptor to remove.</param>
        public static void RemoveInterceptor(InterceptHandler.InterceptorHandler interceptorCall)
        {
            InterceptManager.RemoveInterceptorCall(interceptorCall);
        }

        /// <summary>
        /// Check if interceptor is loaded.
        /// </summary>
        /// <param name="interceptorCall">Intercept to check.</param>
        /// <returns>Returns true if loaded, otherwise returns false.</returns>
        public static bool HasInterceptor(InterceptHandler.InterceptorHandler interceptorCall)
        {
            return InterceptManager.HasInteceptorCall(interceptorCall);
        }

        /// <summary>
        /// Add a method resolver.
        /// </summary>
        /// <param name="methodResolver">Method resolver to add.</param>
        public static void AddMethodResolver(MethodResolverHandler methodResolver)
        {
            CompileMethodHook.AddHandler(methodResolver);

            if (!IsEnabled)
                EnableJitex();
        }

        /// <summary>
        /// Add a token resolver.
        /// </summary>
        /// <param name="tokenResolver">Token resolver to add.</param>
        public static void AddTokenResolver(TokenResolverHandler tokenResolver)
        {
            TokenHook.AddHandler(tokenResolver);

            if (!IsEnabled)
                EnableJitex();
        }

        /// <summary>
        /// Remove a method resolver.
        /// </summary>
        /// <param name="methodResolver">Method resolver to remove.</param>
        public static void RemoveMethodResolver(MethodResolverHandler methodResolver)
        {
            CompileMethodHook.RemoverHandler(methodResolver);
        }

        /// <summary>
        /// Remove a token resolver.
        /// </summary>
        /// <param name="tokenResolver">Token resolver to remove.</param>
        public static void RemoveTokenResolver(TokenResolverHandler tokenResolver)
        {
            TokenHook.RemoverHandler(tokenResolver);
        }

        /// <summary>
        /// Added event to raise after method was compiled
        /// </summary>
        /// <param name="onMethodCompiled"></param>
        public static void AddOnMethodCompiled(MethodCompiledHandler onMethodCompiled)
        {
            CompileMethodHook.AddOnMethodCompiledEvent(onMethodCompiled);
        }

        /// <summary>
        /// Remove event after method was compiled.
        /// </summary>
        /// <param name="onMethodCompiled"></param>
        public static void RemoveOnMethodCompiled(MethodCompiledHandler onMethodCompiled)
        {
            CompileMethodHook.RemoveOnMethodCompiledEvent(onMethodCompiled);
        }

        /// <summary>
        /// Add string resolver.
        /// </summary>
        /// <param name="stringResolver"></param>
        public static void AddStringResolver(StringResolverHandler stringResolver)
        {
            StringHook.AddHandler(stringResolver);
        }

        /// <summary>
        /// Remove string resolver.
        /// </summary>
        /// <param name="stringResolver"></param>
        public static void RemoveStringResolver(StringResolverHandler stringResolver)
        {
            StringHook.RemoverHandler(stringResolver);
        }

        /// <summary>
        /// Returns if a method resolver is already loaded.
        /// </summary>
        /// <param name="methodResolver">Method resolver.</param>
        /// <returns>True to already loaded. False to not loaded.</returns>
        public static bool HasMethodResolver(MethodResolverHandler methodResolver)
        {
            return CompileMethodHook.HasHandler(methodResolver);
        }

        /// <summary>
        /// Returns If a token resolver is already loaded.
        /// </summary>
        /// <param name="tokenResolver">Token resolver.</param>
        /// <returns>True to already loaded. False to not loaded.</returns>
        public static bool HasTokenResolver(TokenResolverHandler tokenResolver)
        {
            return TokenHook.HasHandler(tokenResolver);
        }

        /// <summary>
        /// Unload Jitex and modules from application.
        /// </summary>
        public static bool TryGetModule<TModule>(out TModule? instance)
            where TModule : JitexModule
        {
            if (!ModulesLoaded.TryGetValue(typeof(TModule), out JitexModule loadedInstance))
            {
                instance = null;
                return false;
            }

            instance = (TModule)loadedInstance;
            return true;
        }

        public static void Remove()
        {
            CompileMethodHook.Dispose();
            TokenHook.Dispose();
            StringHook.Dispose();
        }
    }
}