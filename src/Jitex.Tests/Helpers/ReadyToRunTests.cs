using System;
using System.Reflection;
using System.Reflection.Emit;
using Jitex.JIT.Context;
using Jitex.Utils;
using Xunit;


namespace Jitex.Tests.Helpers
{
#if !NETCOREAPP2
    public class ReadyToRunTests
    {
        private static bool _isGetterCompiled;

        static ReadyToRunTests()
        {
            JitexManager.AddMethodResolver(MethodResolver);
        }

        [Fact]
        public void DetectMethodIsReadyToRunTest()
        {
            MethodBase getIlGenerator = typeof(DynamicMethod).GetMethod("GetILGenerator", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);

            bool isReadyToRun = MethodHelper.IsReadyToRun(getIlGenerator);
            Assert.True(isReadyToRun);
        }

        [Fact]
        public void DetectMethodIsNotReadyToRunTest()
        {
            MethodBase methodNotReadyToRun = Utils.GetMethod<ReadyToRunTests>(nameof(MethodNotR2R));

            bool isReadyToRun = MethodHelper.IsReadyToRun(methodNotReadyToRun);
            Assert.False(isReadyToRun);
        }

        [Fact]
        public void DisableReadyToRun()
        {
            MethodBase box = Utils.GetMethod(typeof(AppContext), "get_TargetFrameworkName");
            bool disabled = MethodHelper.DisableReadyToRun(box);
            Assert.True(disabled);

            _ = AppContext.TargetFrameworkName;
            Assert.True(_isGetterCompiled, "Method was not compiled.");
        }

        public void MethodNotR2R() { }

        private static void MethodResolver(MethodContext context)
        {
            if (context.Method.Name == "get_TargetFrameworkName")
                _isGetterCompiled = true;
        }
    }
#endif
}
