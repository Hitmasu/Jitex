using System;
using Jitex.Framework;
using Jitex.JIT.Context;
using Xunit;

namespace Jitex.Tests.Resolvers
{
    [Collection("Manager")]
    public class ResolveStringTests
    {
        static ResolveStringTests()
        {
            JitexManager.AddMethodResolver(context => { });
            JitexManager.AddTokenResolver(TokenResolver);
        }

        [Fact]
        public void ResolveString()
        {
            if (RuntimeFramework.Framework.FrameworkVersion >= new Version(8, 0, 0))
                return;
            
            string text = "Lorem ipsum dolor";
            Assert.True(text == "A fox jump over lazy dog", "String not replaced!");
        }

        private static void TokenResolver(TokenContext context)
        {
            if (context.Content == "Lorem ipsum dolor")
                context.ResolveString("A fox jump over lazy dog");
        }
    }
}
