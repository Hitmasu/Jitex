using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using Jitex.Framework;
using Jitex.Utils;
using Xunit;

namespace Jitex.Tests.Helpers
{
    [Collection("Helpres")]
    [SuppressMessage("Usage", "xUnit1000:Test classes must be public", Justification = "We need wrote better tests for ReadyToRun.")]
    internal class ReadyToRunTests
    {
        [Fact]
        public void DetectMethodIsReadyToRunTest()
        {
            if (RuntimeFramework.Framework.FrameworkVersion < new Version(3, 0, 0))
                return;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return;

            MethodBase getIlGenerator = typeof(DynamicMethod).GetMethod("GetILGenerator",
                BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);

            bool isReadyToRun = MethodHelper.IsReadyToRun(getIlGenerator);
            Assert.True(isReadyToRun);
        }

        [Fact]
        public void DetectMethodIsNotReadyToRunTest()
        {
            if (RuntimeFramework.Framework.FrameworkVersion < new Version(3, 0, 0))
                return;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return;

            MethodBase methodNotReadyToRun = Utils.GetMethod<ReadyToRunTests>(nameof(MethodNotR2R));

            bool isReadyToRun = MethodHelper.IsReadyToRun(methodNotReadyToRun);
            Assert.False(isReadyToRun);
        }

        [Fact]
        public void DisableReadyToRun()
        {
            if (RuntimeFramework.Framework.FrameworkVersion <= new Version(3, 0))
                return;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return;

            MethodBase clampMethod = typeof(Math).GetMethod("Clamp", new[] { typeof(int), typeof(int), typeof(int) });
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