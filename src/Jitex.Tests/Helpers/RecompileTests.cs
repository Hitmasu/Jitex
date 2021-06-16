using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Jitex.JIT.Context;
using Jitex.Tests.Helpers.Attributes;
using Jitex.Tests.Helpers.Recompile;
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
            MethodInfo method = Utils.GetMethod<NonGenericInstanceClass>(nameof(NonGenericInstanceClass.NonGeneric));
            NonGenericInstanceClass instance = new NonGenericInstanceClass();

            instance.NonGeneric();

            MethodHelper.ForceRecompile(method);

            instance.NonGeneric();

            int count = MethodsCompiled.Count(m => m == method);

            Assert.Equal(2, count);
        }

        [Fact]
        public void MethodInstanceGenericPrimitiveTest()
        {
            MethodInfo method = Utils.GetMethod<NonGenericInstanceClass>(nameof(NonGenericInstanceClass.GenericPrimitive));
            method = method.MakeGenericMethod(typeof(int));

            NonGenericInstanceClass instance = new NonGenericInstanceClass();

            instance.GenericPrimitive<int>();

            MethodHelper.ForceRecompile(method);

            instance.GenericPrimitive<int>();

            int count = MethodsCompiled.Count(m => m.MetadataToken == method.MetadataToken);

            Assert.Equal(2, count);
        }

        [Fact]
        public void MethodInstanceGenericCanonTest()
        {
            MethodInfo method = Utils.GetMethod<NonGenericInstanceClass>(nameof(NonGenericInstanceClass.GenericCanon));
            method = method.MakeGenericMethod(typeof(RecompileTests));

            NonGenericInstanceClass instance = new NonGenericInstanceClass();

            instance.GenericCanon<RecompileTests>();

            MethodHelper.ForceRecompile(method);

            instance.GenericCanon<RecompileTests>();

            int count = MethodsCompiled.Count(m => m.MetadataToken == method.MetadataToken);

            Assert.Equal(2, count);
        }

        [Fact]
        public void MethodStaticOnNonStaticTypeTest()
        {
            MethodInfo method = Utils.GetMethod<NonGenericInstanceClass>(nameof(NonGenericInstanceClass.StaticNonGeneric));

            NonGenericInstanceClass.StaticNonGeneric();

            MethodHelper.ForceRecompile(method);

            NonGenericInstanceClass.StaticNonGeneric();

            int count = MethodsCompiled.Count(m => m == method);

            Assert.Equal(2, count);
        }

        [Fact]
        public void MethodStaticGenericPrimitiveOnNonStaticTypeTest()
        {
            MethodInfo method = Utils.GetMethod<NonGenericInstanceClass>(nameof(NonGenericInstanceClass.StaticGenericPrimitive));
            method = method.MakeGenericMethod(typeof(int));

            NonGenericInstanceClass.StaticGenericPrimitive<int>();

            MethodHelper.ForceRecompile(method);

            NonGenericInstanceClass.StaticGenericPrimitive<int>();

            int count = MethodsCompiled.Count(m => m.MetadataToken == method.MetadataToken);

            Assert.Equal(2, count);
        }

        [Fact]
        public void MethodStaticGenericCanonOnNonStaticTypeTest()
        {
            MethodInfo method = Utils.GetMethod<NonGenericInstanceClass>(nameof(NonGenericInstanceClass.StaticGenericCanon));
            method = method.MakeGenericMethod(typeof(RecompileTests));

            NonGenericInstanceClass.StaticGenericCanon<RecompileTests>();

            MethodHelper.ForceRecompile(method);

            NonGenericInstanceClass.StaticGenericCanon<RecompileTests>();

            int count = MethodsCompiled.Count(m => m.MetadataToken == method.MetadataToken);

            Assert.Equal(2, count);
        }

        [Fact]
        public void MethodStaticNonGenericTest()
        {
            MethodInfo method = Utils.GetMethod(typeof(NonGenericStaticClass), nameof(NonGenericStaticClass.NonGeneric));

            NonGenericStaticClass.NonGeneric();

            MethodHelper.ForceRecompile(method);

            NonGenericStaticClass.NonGeneric();

            int count = MethodsCompiled.Count(m => m.MetadataToken == method.MetadataToken);

            Assert.Equal(2, count);
        }

        [Fact]
        public void MethodStaticGenericPrimitiveTest()
        {
            MethodInfo method = Utils.GetMethod(typeof(NonGenericStaticClass), nameof(NonGenericStaticClass.GenericPrimitive));
            method = method.MakeGenericMethod(typeof(int));

            NonGenericStaticClass.GenericPrimitive<int>();

            MethodHelper.ForceRecompile(method);

            NonGenericStaticClass.GenericPrimitive<int>();

            int count = MethodsCompiled.Count(m => m.MetadataToken == method.MetadataToken);

            Assert.Equal(2, count);
        }

        [Fact]
        public void MethodStaticGenericCanonTest()
        {
            MethodInfo method = Utils.GetMethod(typeof(NonGenericStaticClass), nameof(NonGenericStaticClass.GenericCanon));
            method = method.MakeGenericMethod(typeof(RecompileTests));

            NonGenericStaticClass.GenericCanon<RecompileTests>();

            MethodHelper.ForceRecompile(method);

            NonGenericStaticClass.GenericCanon<RecompileTests>();

            int count = MethodsCompiled.Count(m => m.MetadataToken == method.MetadataToken);

            Assert.Equal(2, count);
        }

        private static void MethodResolver(MethodContext context)
        {
            if (context.Method.DeclaringType != null &&
                context.Method.DeclaringType.GetCustomAttribute<ClassRecompileTestAttribute>() != null)
                MethodsCompiled.Add(context.Method);
        }
    }
}
