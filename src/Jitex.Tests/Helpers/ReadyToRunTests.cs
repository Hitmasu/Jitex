using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using Jitex.Utils;
using Xunit;

namespace Jitex.Tests.Helpers
{
    public class ReadyToRunTests
    {
        private static readonly MethodBase GetIlGenerator;
        private static readonly MethodBase MethodNotReadyToRun;

        static ReadyToRunTests()
        {
            GetIlGenerator = typeof(DynamicMethod).GetMethod("GetILGenerator", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
            MethodNotReadyToRun = typeof(ReadyToRunTests).GetMethod(nameof(MethodNotR2R), BindingFlags.Public | BindingFlags.Instance);
        }

        [Fact]
        public void DetectMethodIsReadyToRunTest()
        {
            bool isReadyToRun = MethodHelper.IsReadyToRun(GetIlGenerator);
            Assert.True(isReadyToRun);
        }

        [Fact]
        public void DetectMethodIsNotReadyToRunTest()
        {
            bool isReadyToRun = MethodHelper.IsReadyToRun(MethodNotReadyToRun);
            Assert.False(isReadyToRun);
        }

        public void MethodNotR2R() { }
    }
}
