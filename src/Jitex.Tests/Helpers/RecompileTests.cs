using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Jitex.JIT.Context;
using Jitex.Tests.Helpers.Attributes;
using Jitex.Tests.Helpers.Recompile;
using Jitex.Utils;
using Xunit;
using Xunit.Abstractions;
using static Jitex.Tests.LogOutput;

namespace Jitex.Tests.Helpers
{
    #if NET5_0
    public class RecompileTests
    {
        private static IList<MethodBase> MethodsCompiled { get; } = new List<MethodBase>();

        public RecompileTests(ITestOutputHelper output)
        {
            Output = output;
        }

        static RecompileTests()
        {
            JitexManager.AddMethodResolver(MethodResolver);
        }

        [Fact]
        public void MethodInstanceNonGenericTest()
        {
            MethodInfo method = Utils.GetMethod<NonGenericInstanceClass>(nameof(NonGenericInstanceClass.NonGeneric));
            NonGenericInstanceClass instance = new NonGenericInstanceClass();

            instance.NonGeneric();

            MethodHelper.ForceRecompile(method);

            instance.NonGeneric();

            int count = MethodsCompiled.ToList().Count(m => m == method);

            Assert.Equal(2, count);
        }

        [Theory]
        [InlineData(typeof(int))]
        [InlineData(typeof(RecompileTests))]
        public void MethodInstanceGenericTest(Type type)
        {
            MethodInfo method = Utils.GetMethod<NonGenericInstanceClass>(nameof(NonGenericInstanceClass.Generic));
            method = method.MakeGenericMethod(type);

            NonGenericInstanceClass instance = new NonGenericInstanceClass();
            method.Invoke(instance, null);

            MethodHelper.ForceRecompile(method);

            method.Invoke(instance, null);

            method = (MethodInfo)MethodHelper.GetOriginalMethod(method);
            int count = MethodsCompiled.ToList().Count(m => m == method);

            Assert.Equal(2, count);
        }

        [Fact]
        public void MethodStaticOnNonStaticTypeTest()
        {
            MethodInfo method = Utils.GetMethod<NonGenericInstanceClass>(nameof(NonGenericInstanceClass.StaticNonGeneric));

            NonGenericInstanceClass.StaticNonGeneric();

            MethodHelper.ForceRecompile(method);

            NonGenericInstanceClass.StaticNonGeneric();

            int count = MethodsCompiled.ToList().Count(m => m == method);

            Assert.Equal(2, count);
        }

        [Theory]
        [InlineData(typeof(int))]
        [InlineData(typeof(RecompileTests))]
        public void MethodStaticGenericOnNonStaticTypeTest(Type type)
        {
            MethodInfo method = Utils.GetMethod<NonGenericInstanceClass>(nameof(NonGenericInstanceClass.StaticGeneric));
            method = method.MakeGenericMethod(type);
            method.Invoke(null, null);

            MethodHelper.ForceRecompile(method);

            method.Invoke(null, null);

            method = (MethodInfo)MethodHelper.GetOriginalMethod(method);
            int count = MethodsCompiled.ToList().Count(m => m == method);

            Assert.Equal(2, count);
        }

        [Fact]
        public void MethodStaticNonGenericTest()
        {
            MethodInfo method = Utils.GetMethod(typeof(NonGenericStaticClass), nameof(NonGenericStaticClass.NonGeneric));

            NonGenericStaticClass.NonGeneric();

            MethodHelper.ForceRecompile(method);

            NonGenericStaticClass.NonGeneric();

            int count = MethodsCompiled.ToList().Count(m => m == method);

            Assert.Equal(2, count);
        }

        [Theory]
        [InlineData(typeof(int))]
        [InlineData(typeof(RecompileTests))]
        public void MethodStaticGenericTest(Type type)
        {
            MethodInfo method = Utils.GetMethod(typeof(NonGenericStaticClass), nameof(NonGenericStaticClass.Generic));
            method = method.MakeGenericMethod(type);

            method.Invoke(null, null);

            MethodHelper.ForceRecompile(method);

            method.Invoke(null, null);

            method = (MethodInfo)MethodHelper.GetOriginalMethod(method);
            int count = MethodsCompiled.ToList().Count(m => m == method);

            Assert.Equal(2, count);
        }

        private static void MethodResolver(MethodContext context)
        {
            if (context.Method.DeclaringType != null &&
                context.Method.DeclaringType.GetCustomAttribute<ClassRecompileTestAttribute>() != null)
                MethodsCompiled.Add(context.Method);
        }
    }
    #endif   
}