using System;
using System.Collections.Generic;
using System.Text;
using Jitex.JIT.Context;
using Xunit;
using Xunit.Extensions.Ordering;

namespace Jitex.Tests.Manager
{
    public class ManagerTest
    {
        [Fact, Order(0)]
        public void LoadJitexTest()
        {
            JitexManager.AddMethodResolver(MethodResolver);
            JitexManager.AddTokenResolver(TokenResolver);
            Assert.True(JitexManager.IsLoaded, "Jitex not loaded!");
        }

        [Fact, Order(1)]
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

        [Fact, Order(3)]
        public void RemoveTest()
        {
             JitexManager.Remove();
             Assert.False(JitexManager.IsLoaded, "Jitex still loaded!");
        }

        private void MethodResolver(MethodContext context)
        {
            //throw new NotImplementedException();
        }

        private void TokenResolver(TokenContext context)
        {
            //throw new NotImplementedException();
        }

    }
}
