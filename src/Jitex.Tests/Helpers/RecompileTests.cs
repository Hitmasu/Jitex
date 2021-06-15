using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Jitex.JIT.Context;
using Jitex.Utils;
using Xunit;

namespace Jitex.Tests.Helpers
{
    public class RecompileTests
    {
        private static IList<MethodBase> MethodsCompiled { get; } = new List<MethodBase>();

        static RecompileTests()
        {
            JitexManager.AddMethodResolver(MethodResolver);
        }

        [Fact]
        public void MethodInstanceNonGenericTest()
        {
            SimpleInstanceMethod();

            MethodInfo method = Utils.GetMethod<RecompileTests>(nameof(SimpleInstanceMethod));
            MethodHelper.ForceRecompile(method);

            SimpleInstanceMethod();

            int count = MethodsCompiled.Count(m => m == method);

            Assert.Equal(2, count);
        }

        [Fact]
        public void MethodInstanceGenericPrimitiveTest()
        {
            SimpleInstanceGenericPrimitiveMethod<int>();

            MethodInfo method = Utils.GetMethod<RecompileTests>(nameof(SimpleInstanceGenericPrimitiveMethod));
            method = method.MakeGenericMethod(typeof(int));
            MethodHelper.ForceRecompile(method);

            SimpleInstanceGenericPrimitiveMethod<int>();

            int count = MethodsCompiled.Count(m => m.MetadataToken == method.MetadataToken);

            Assert.Equal(2, count);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void SimpleInstanceMethod() { }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void SimpleInstanceGenericPrimitiveMethod<T>() { }

        private static void MethodResolver(MethodContext context)
        {
            if (context.Method.DeclaringType == typeof(RecompileTests))
                MethodsCompiled.Add(context.Method);
        }
    }
}
