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

        static GenericRecompileTests()
        {
            JitexManager.AddMethodResolver(MethodResolver);
        }

        [Theory]
        [InlineData(typeof(int))]
        [InlineData(typeof(GenericRecompileTests))]
        public void MethodInstanceNonGenericTest(Type type)
        {
#if !NET5_0
            return;
#endif
            Type baseType = typeof(GenericInstanceClass<>);
            MethodInfo method = Utils.GetMethodInfo(baseType, type, "NonGeneric");
            object instance = Utils.CreateInstance(baseType, type);

            method.Invoke(instance, null);

            MethodHelper.ForceRecompile(method);

            method.Invoke(instance, null);

            method = (MethodInfo) MethodHelper.GetOriginalMethod(method);
            int count = MethodsCompiled.ToList().Count(m => m == method);

            Assert.Equal(2, count);
        }

        [Theory]
        [InlineData(typeof(int), typeof(int))]
        [InlineData(typeof(GenericRecompileTests), typeof(int))]
        [InlineData(typeof(int), typeof(GenericRecompileTests))]
        [InlineData(typeof(GenericRecompileTests), typeof(GenericRecompileTests))]
        public void MethodInstanceGenericTest(Type baseTypeGenericParameter, Type methodGenericParameter)
        {
#if !NET5_0
            return;
#endif
            Type baseType = typeof(GenericInstanceClass<>);
            MethodInfo method = Utils.GetMethodInfo(baseType, baseTypeGenericParameter, "Generic");
            method = method.MakeGenericMethod(methodGenericParameter);
            object instance = Utils.CreateInstance(baseType, baseTypeGenericParameter);

            method.Invoke(instance, null);

            MethodHelper.ForceRecompile(method);

            method.Invoke(instance, null);

            method = (MethodInfo) MethodHelper.GetOriginalMethod(method);
            int count = MethodsCompiled.ToList().Count(m => m == method);

            Assert.Equal(2, count);
        }

        [Theory]
        [InlineData(typeof(int))]
        [InlineData(typeof(GenericRecompileTests))]
        public void MethodStaticNonGenericOnNonStaticType(Type type)
        {
#if !NET5_0
            return;
#endif
            Type baseType = typeof(GenericInstanceClass<>);
            MethodInfo method = Utils.GetMethodInfo(baseType, type, "StaticNonGeneric");
            method.Invoke(null, null);

            MethodHelper.ForceRecompile(method);

            method.Invoke(null, null);

            method = (MethodInfo) MethodHelper.GetOriginalMethod(method);
            int count = MethodsCompiled.ToList().Count(m => m == method);

            Assert.Equal(2, count);
        }

        [Theory]
        [InlineData(typeof(int), typeof(int))]
        [InlineData(typeof(GenericRecompileTests), typeof(int))]
        [InlineData(typeof(int), typeof(GenericRecompileTests))]
        [InlineData(typeof(GenericRecompileTests), typeof(GenericRecompileTests))]
        public void MethodStaticGenericOnNonStaticType(Type baseTypeGenericParameter, Type methodGenericParameter)
        {
#if !NET5_0
            return;
#endif
            Type baseType = typeof(GenericInstanceClass<>);
            MethodInfo method = Utils.GetMethodInfo(baseType, baseTypeGenericParameter, "StaticGeneric");
            method = method.MakeGenericMethod(methodGenericParameter);

            method.Invoke(null, null);

            MethodHelper.ForceRecompile(method);

            method.Invoke(null, null);

            method = (MethodInfo) MethodHelper.GetOriginalMethod(method);
            int count = MethodsCompiled.ToList().Count(m => m == method);

            Assert.Equal(2, count);
        }

        [Theory]
        [InlineData(typeof(int))]
        [InlineData(typeof(GenericRecompileTests))]
        public void MethodStaticNonGenericTest(Type type)
        {
#if !NET5_0
            return;
#endif
            Type baseType = typeof(GenericStaticClass<>);
            MethodInfo method = Utils.GetMethodInfo(baseType, type, "NonGeneric");

            method.Invoke(null, null);

            MethodHelper.ForceRecompile(method);

            method.Invoke(null, null);

            method = (MethodInfo) MethodHelper.GetOriginalMethod(method);
            int count = MethodsCompiled.ToList().Count(m => m == method);

            Assert.Equal(2, count);
        }

        [Theory]
        [InlineData(typeof(int), typeof(int))]
        [InlineData(typeof(GenericRecompileTests), typeof(int))]
        [InlineData(typeof(int), typeof(GenericRecompileTests))]
        [InlineData(typeof(GenericRecompileTests), typeof(GenericRecompileTests))]
        public void MethodStaticGenericTest(Type baseTypeGenericParameter, Type methodGenericParameter)
        {
#if !NET5_0
            return;
#endif
            Type baseType = typeof(GenericStaticClass<>);
            MethodInfo method = Utils.GetMethodInfo(baseType, baseTypeGenericParameter, "Generic");
            method = method.MakeGenericMethod(methodGenericParameter);

            method.Invoke(null, null);

            MethodHelper.ForceRecompile(method);

            method.Invoke(null, null);

            method = (MethodInfo) MethodHelper.GetOriginalMethod(method);
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
}