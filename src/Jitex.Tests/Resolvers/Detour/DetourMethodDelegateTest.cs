using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Jitex.JIT.Context;
using Jitex.Utils;
using Xunit;

namespace Jitex.Tests.Detour
{
    [Collection("Manager")]
    public class DetourMethodDelegateTest
    {
        private static readonly IList<string> Trace = new List<string>();
        private static readonly object Lock = new object();

        static DetourMethodDelegateTest()
        {
            JitexManager.AddMethodResolver(MethodResolver);
        }

        [Fact]
        public void DetourActionTest()
        {
            if (OSHelper.IsHardenedRuntime)
                return;
            
            SimpleMethod();
            Assert.True(Trace.Contains(nameof(SimpleMethod) + "Detour"), "Detour not called!");
        }

        [Fact]
        public void DetourActionByGenericTest()
        {
            if (OSHelper.IsHardenedRuntime)
                return;
            
            SimpleMethod2();
            Assert.True(Trace.Contains(nameof(SimpleMethod2) + "Detour"), "Detour not called!");
        }

        [Fact]
        public void DetourFuncTest()
        {
            if (OSHelper.IsHardenedRuntime)
                return;
            
            int result = Sum(5, 5);
            Assert.True(result == 25, "Detour not called!");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void SimpleMethod()
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void SimpleMethod2()
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public int Sum(int n1, int n2) => n1 + n2;

        private static void AddMethodCalled(string detourMethod = "")
        {
            lock (Lock)
            {
                Trace.Add(detourMethod);
            }
        }

        private static void MethodResolver(MethodContext context)
        {
            if (context.Method == Utils.GetMethod<DetourMethodDelegateTest>(nameof(SimpleMethod)))
            {
                Action action = () => { AddMethodCalled(nameof(SimpleMethod) + "Detour"); };

                context.ResolveDetour(action);
            }
            else if (context.Method == Utils.GetMethod<DetourMethodDelegateTest>(nameof(SimpleMethod2)))
            {
                context.ResolveDetour<Action>(() => { AddMethodCalled(nameof(SimpleMethod2) + "Detour"); });
            }
            else if (context.Method == Utils.GetMethod<DetourMethodDelegateTest>(nameof(Sum)))
            {
                context.ResolveDetour<Func<int, int, int>>((n1, n2) => n1 * n2);
            }
        }
    }
}