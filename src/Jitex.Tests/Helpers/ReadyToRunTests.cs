using System;
using System.Reflection;
using System.Reflection.Emit;
using Jitex.Utils;
using Xunit;


namespace Jitex.Tests.Helpers
{
    public class ReadyToRunTests
    {
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
            MethodBase clampMethod = typeof(Math).GetMethod("Clamp", new[] { typeof(int), typeof(int), typeof(int) });
            bool disabled = MethodHelper.DisableReadyToRun(clampMethod);
            Assert.True(disabled);

            bool isClampCompiled = false;

            JitexManager.AddMethodResolver(context =>
            {
                if (context.Method == clampMethod)
                    isClampCompiled = true;
            });

            Math.Clamp(1, 1, 1);
            Assert.True(isClampCompiled, "Method was not compiled.");
        }

        public void MethodNotR2R() { }
    }
}
