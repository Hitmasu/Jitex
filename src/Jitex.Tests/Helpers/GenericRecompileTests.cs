using Jitex.JIT.Context;
using Jitex.Tests.Helpers.Attributes;
using Jitex.Tests.Helpers.Recompile;
using Jitex.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Jitex.Tests.Helpers
{
    public class GenericRecompileTests
    {
        private static IList<MethodBase> MethodsCompiled { get; } = new List<MethodBase>();
        private readonly Type baseType = typeof(GenericInstanceClass<>);

        static GenericRecompileTests()
        {
            JitexManager.AddMethodResolver(MethodResolver);
        }

        [Theory]
        [InlineData(typeof(int))]
        [InlineData(typeof(GenericRecompileTests))]
        public void MethodInstanceNonGenericTest(Type type)
        {
            MethodInfo method = Utils.GetMethodInfo(baseType, type, "NonGeneric");
            object instance = Utils.CreateInstance(baseType, type);

            method.Invoke(instance, null);

            MethodHelper.ForceRecompile(method);

            method.Invoke(instance, null);

            method = (MethodInfo)MethodHelper.GetOriginalMethod(method);
            int count = MethodsCompiled.Count(m => m == method);

            Assert.Equal(2, count);
        }

        [Theory]
        [InlineData(typeof(int), typeof(int))]
        [InlineData(typeof(GenericRecompileTests), typeof(int))]
        [InlineData(typeof(int), typeof(GenericRecompileTests))]
        [InlineData(typeof(GenericRecompileTests), typeof(GenericRecompileTests))]
        public void MethodInstanceGenericTest(Type baseTypeGenericParameter, Type methodGenericParameter)
        {
            MethodInfo method = Utils.GetMethodInfo(baseType, baseTypeGenericParameter, "Generic");
            method = method.MakeGenericMethod(methodGenericParameter);

            object instance = Utils.CreateInstance(baseType, baseTypeGenericParameter);

            method.Invoke(instance, null);

            MethodHelper.ForceRecompile(method);

            method.Invoke(instance, null);

            method = (MethodInfo)MethodHelper.GetOriginalMethod(method);
            int count = MethodsCompiled.Count(m => m == method);

            Assert.Equal(2, count);
        }

        //[Fact]
        //public void MethodStaticGenericPrimitiveOnNonStaticTypeTest()
        //{
        //    MethodInfo method = Utils.GetMethod<NonGenericInstanceClass>(nameof(NonGenericInstanceClass.StaticGenericPrimitive));
        //    method = method.MakeGenericMethod(typeof(int));

        //    NonGenericInstanceClass.StaticGenericPrimitive<int>();

        //    MethodHelper.ForceRecompile(method);

        //    NonGenericInstanceClass.StaticGenericPrimitive<int>();

        //    int count = MethodsCompiled.Count(m => m.MetadataToken == method.MetadataToken);

        //    Assert.Equal(2, count);
        //}

        //[Fact]
        //public void MethodStaticGenericCanonOnNonStaticTypeTest()
        //{
        //    MethodInfo method = Utils.GetMethod<NonGenericInstanceClass>(nameof(NonGenericInstanceClass.StaticGenericCanon));
        //    method = method.MakeGenericMethod(typeof(RecompileTests));

        //    NonGenericInstanceClass.StaticGenericCanon<RecompileTests>();

        //    MethodHelper.ForceRecompile(method);

        //    NonGenericInstanceClass.StaticGenericCanon<RecompileTests>();

        //    int count = MethodsCompiled.Count(m => m.MetadataToken == method.MetadataToken);

        //    Assert.Equal(2, count);
        //}

        private static void MethodResolver(MethodContext context)
        {
            if (context.Method.DeclaringType != null &&
                context.Method.DeclaringType.GetCustomAttribute<ClassRecompileTestAttribute>() != null)
                MethodsCompiled.Add(context.Method);
        }
    }
}
