using System;
using System.Reflection;
using Jitex.Tests.Modules;
using Xunit;
using Xunit.Extensions.Ordering;
using static Jitex.Tests.Utils;

namespace Jitex.Tests
{
    public class ModuleTests
    {

        [Fact, Order(1)]
        public void ModuleLoadTest()
        {
            JitexManager.LoadModule<ModuleJitex>();
            bool moduleIsLoaded = JitexManager.ModuleIsLoaded<ModuleJitex>();
            Assert.True(moduleIsLoaded, "Module is not loaded!");
        }

        [Fact, Order(2)]
        public void ResolveMethodTest()
        {
            MethodInfo method = GetMethod<ModuleTests>(nameof(MethodToCompileOnLoad));
            MethodToCompileOnLoad();
            bool called = ModuleJitex.MethodsCompiled.Contains(method);
            Assert.True(called, "Resolver method not called!");
        }

        [Fact, Order(3)]
        public void ResolveTokenTest()
        {
            MethodInfo method = GetMethod<ModuleTests>(nameof(TokenToCompileOnLoad));
            MethodToCallTokenOnLoad();
            bool called = ModuleJitex.TokensCompiled.Contains(method.MetadataToken);
            Assert.True(called, "Resolver token not called!");
        }

        [Fact, Order(4)]
        public void RemoveModuleTest()
        {
            JitexManager.RemoveModule<ModuleJitex>();
            bool moduleIsLoaded = JitexManager.ModuleIsLoaded<ModuleJitex>();
            Assert.False(moduleIsLoaded, "Module still loaded!");
        }

        [Fact, Order(5)]
        public void RemoveMethodTest()
        {
            ModuleLoadTest();
            JitexManager.RemoveModule<ModuleJitex>();

            MethodInfo method = GetMethod<ModuleTests>(nameof(MethodToCompileOnRemove));
            MethodToCompileOnRemove();
            bool called = ModuleJitex.MethodsCompiled.Contains(method);
            Assert.False(called, "Resolver method called!");
        }

        [Fact, Order(6)]
        public void RemoveTokenTest()
        {
            ModuleLoadTest();
            JitexManager.RemoveModule<ModuleJitex>();

            MethodInfo method = GetMethod<ModuleTests>(nameof(TokenToCompileOnRemove));
            MethodToCallTokenOnRemove();
            bool called = ModuleJitex.TokensCompiled.Contains(method.MetadataToken);
            Assert.False(called, "Resolver token called!");
        }

        /// <summary>
        /// Method just to call resolver.
        /// </summary>
        public void MethodToCompileOnLoad()
        {
        }

        /// <summary>
        /// Method just to call resolver.
        /// </summary>
        public void MethodToCompileOnRemove()
        {
        }

        /// <summary>
        /// Method just to call resolver.
        /// </summary>
        public void TokenToCompileOnLoad()
        {
        }

        /// <summary>
        /// Method just to call resolver.
        /// </summary>
        public void TokenToCompileOnRemove()
        {
        }

        /// <summary>
        /// Method just to call token.
        /// </summary>
        public void MethodToCallTokenOnLoad()
        {
            TokenToCompileOnLoad();
        }

        /// <summary>
        /// Method just to call token.
        /// </summary>
        public void MethodToCallTokenOnRemove()
        {
            TokenToCompileOnRemove();
        }
    }
}
