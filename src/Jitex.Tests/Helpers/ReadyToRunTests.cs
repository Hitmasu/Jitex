using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Jitex.Utils;
using Xunit;


namespace Jitex.Tests.Helpers
{
    public class ReadyToRunTests
    {
        [Fact]
        public void DetectMethodIsReadyToRunTest()
        {
#if NETCOREAPP2
            return;
#endif
            MethodBase getIlGenerator = typeof(DynamicMethod).GetMethod("GetILGenerator", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);

            bool isReadyToRun = MethodHelper.IsReadyToRun(getIlGenerator);
            Assert.True(isReadyToRun);
        }

        [Fact]
        public void DetectMethodIsNotReadyToRunTest()
        {
#if NETCOREAPP2
            return;
#endif
            MethodBase methodNotReadyToRun = Utils.GetMethod<ReadyToRunTests>(nameof(MethodNotR2R));

            bool isReadyToRun = MethodHelper.IsReadyToRun(methodNotReadyToRun);
            Assert.False(isReadyToRun);
        }

        [Fact]
        public void DisableReadyToRun()
        {
#if NETCOREAPP2 || NETCOREAPP3_0
            return;
#endif
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return;

            MethodBase clampMethod = typeof(Math).GetMethod("Clamp", new[] {typeof(int), typeof(int), typeof(int)});
            bool disabled = MethodHelper.DisableReadyToRun(clampMethod);

            Assert.True(disabled);

            bool isClampCompiled = false;

            JitexManager.AddMethodResolver(context =>
            {
                if (context.Method.Name == clampMethod.Name)
                    isClampCompiled = true;
            });

            Math.Clamp(1, 1, 1);

            Assert.True(isClampCompiled, "Method was not compiled.");
        }

        public void MethodNotR2R()
        {
        }
    }
}