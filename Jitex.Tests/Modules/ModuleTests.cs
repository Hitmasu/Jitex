using System.Linq;
using System.Reflection;
using Xunit;
using Xunit.Extensions.Ordering;
using static Jitex.Tests.Utils;

namespace Jitex.Tests.Modules
{
    [Collection("Manager")]
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
        public void MethodResolverTest()
        {
            MethodInfo method = GetMethod<ModuleTests>(nameof(MethodToCompileOnLoad));
            MethodToCompileOnLoad();
            bool called = ModuleJitex.MethodsCompiled.ToList().Count(m => m == method) == 1;
            Assert.True(called, "Method resolver not called!");
        }

        [Fact, Order(2)]
        public void TokenResolverTest()
        {
            MethodInfo method = GetMethod<ModuleTests>(nameof(TokenToCompileOnLoad));
            MethodToCallTokenOnLoad();
            bool called = ModuleJitex.TokensCompiled.ToList().Count(m => m == method.MetadataToken) == 1;
            Assert.True(called, "Token resolver not called!");
        }

        [Fact, Order(3)]
        public void RemoveModuleTest()
        {
            JitexManager.RemoveModule<ModuleJitex>();
            bool moduleIsLoaded = JitexManager.ModuleIsLoaded<ModuleJitex>();
            Assert.False(moduleIsLoaded, "Module still loaded!");
        }

        [Fact, Order(4)]
        public void RemoveMethodResolverTest()
        {
            ModuleLoadTest();
            JitexManager.RemoveModule<ModuleJitex>();

            MethodInfo method = GetMethod<ModuleTests>(nameof(MethodToCompileOnRemove));
            MethodToCompileOnRemove();
            bool called = ModuleJitex.MethodsCompiled.Contains(method);
            Assert.False(called, "Method resolver called!");
        }

        [Fact, Order(5)]
        public void RemoveTokenResolverTest()
        {
            ModuleLoadTest();
            JitexManager.RemoveModule<ModuleJitex>();

            MethodInfo method = GetMethod<ModuleTests>(nameof(TokenToCompileOnRemove));
            MethodToCallTokenOnRemove();
            bool called = ModuleJitex.TokensCompiled.Contains(method.MetadataToken);
            Assert.False(called, "Token resolver called!");
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
