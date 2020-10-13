using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Jitex.JIT.Context;
using Jitex.Tests.Modules;
using Xunit;
using Xunit.Extensions.Ordering;
using static Jitex.Tests.Utils;

namespace Jitex.Tests.Manager
{
    [Collection("Manager")]
    public class ManagerTest
    {
        private static IList<MethodBase> MethodsCompiled { get; } = new List<MethodBase>();
        private static IList<int> TokensCompiled { get; } = new List<int>();

        [Fact, Order(1)]
        public void LoadJitexTest()
        {
            JitexManager.AddMethodResolver(MethodResolver);
            JitexManager.AddTokenResolver(TokenResolver);
            Assert.True(JitexManager.IsLoaded, "Jitex not loaded!");
        }

        [Fact, Order(2)]
        public void HasMethodResolverTest()
        {
            bool hasResolver = JitexManager.HasMethodResolver(MethodResolver);
            Assert.True(hasResolver, "Method resolver not injected!");
        }

        [Fact, Order(2)]
        public void HasTokenResolverTest()
        {
            bool hasResolver = JitexManager.HasTokenResolver(TokenResolver);
            Assert.True(hasResolver, "Token resolver not injected!");
        }

        [Fact, Order(2)]
        public void MethodResolverTest()
        {
            MethodInfo method = GetMethod<ManagerTest>(nameof(MethodToCompileOnLoad));
            MethodToCompileOnLoad();
            bool called = MethodsCompiled.ToList().Count(m => m == method) == 1;
            Assert.True(called, "Method resolver not called!");
        }

        [Fact, Order(2)]
        public void TokenResolverTest()
        {
            MethodInfo method = GetMethod<ManagerTest>(nameof(TokenToCompileOnLoad));
            TokenToCompileOnLoad();

            bool called = TokensCompiled.ToList().Count(m => m == method.MetadataToken) == 1;
            Assert.True(called, "Token resolver not called!");
        }

        [Fact, Order(3)]
        public void RemoveTest()
        {
            LoadJitexTest();
            JitexManager.Remove();
            Assert.False(JitexManager.IsLoaded, "Jitex still loaded!");
        }

        [Fact, Order(4)]
        public void RemoveMethodResolverTest()
        {
            LoadJitexTest();
            JitexManager.Remove();

            MethodInfo method = GetMethod<ModuleTests>(nameof(MethodToCompileOnRemove));

            MethodToCompileOnRemove();
            bool called = MethodsCompiled.Contains(method);
            Assert.False(called, "Method resolver called!");
        }

        [Fact, Order(5)]
        public void RemoveTokenResolverTest()
        {
            LoadJitexTest();
            JitexManager.Remove();

            MethodInfo method = GetMethod<ModuleTests>(nameof(TokenToCompileOnRemove));
            MethodToCallTokenOnRemove();
            bool called = TokensCompiled.Contains(method.MetadataToken);
            Assert.False(called, "Token resolver called!");
        }

        private void MethodResolver(MethodContext context)
        {
            if (context.Method.Module == GetType().Module)
                MethodsCompiled.Add(context.Method);
        }

        private void TokenResolver(TokenContext context)
        {
            if (context.Module == GetType().Module)
                TokensCompiled.Add(context.MetadataToken);
        }

        /// <summary>
        /// Method just to call resolver.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
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
