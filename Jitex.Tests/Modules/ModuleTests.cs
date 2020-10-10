using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Jitex.JIT.Context;
using Jitex.Tests.Modules;
using Xunit;
using static Jitex.Tests.Utils;
namespace Jitex.Tests
{
    public class ModuleTests
    {
        static ModuleTests()
        {
            JitexManager.LoadModule<ModuleJitex>();
        }

        [Fact]
        public void ModuleLoadTest()
        {
            bool moduleIsLoaded = JitexManager.ModuleIsLoaded<ModuleJitex>();
            Assert.True(moduleIsLoaded, "Module is not loaded!");
        }

        [Fact]
        public void ResolveMethodTest()
        {
            MethodInfo method = GetMethod<ModuleTests>(nameof(MethodToCompileOnLoad));
            MethodToCompileOnLoad();
            bool called = ModuleJitex.MethodsCompiled.Contains(method);
            Assert.True(called, "Resolver method not called!");
        }

        [Fact]
        public void ResolveTokenTest()
        {
            MethodInfo method = GetMethod<ModuleTests>(nameof(TokenToCompileOnLoad));
            MethodToCallTokenOnLoad();
            bool called = ModuleJitex.TokensCompiled.Contains(method.MetadataToken);
            Assert.True(called, "Resolver token not called!");
        }

        [Fact]
        public void RemoveModule()
        {
            JitexManager.RemoveModule<JitexModule>();

            MethodInfo method = GetMethod<ModuleTests>(nameof(MethodToCompileOnRemove));
            MethodToCompileOnRemove();
            bool called = ModuleJitex.MethodsCompiled.Contains(method);
            Assert.False(called, "Resolver method called!");

            method = GetMethod<ModuleTests>(nameof(TokenToCompileOnRemove));
            MethodToCallTokenOnRemove();
            called = ModuleJitex.TokensCompiled.Contains(method.MetadataToken);
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
