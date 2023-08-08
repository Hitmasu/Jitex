using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Jitex.JIT.Context;
using Jitex.Tests.Context;
using Jitex.Utils;
using Xunit;

namespace Jitex.Tests.Detour
{
    [Collection("Manager")]
    public class DetourMethodTest
    {
        private static readonly IList<string> Trace = new List<string>();
        private static readonly object Lock = new object();

        static DetourMethodTest()
        {
            JitexManager.AddMethodResolver(MethodResolver);
        }

        [Fact]
        public void SimpleDetourMethodTest()
        {
            if (OSHelper.IsHardenedRuntime)
                return;
            
            SimpleMethod();
            Assert.True(Trace.Contains(nameof(SimpleMethodDetour)), "Detour not called!");
        }

        [Fact]
        public void DetourMethodParametersTest()
        {
            if (OSHelper.IsHardenedRuntime)
                return;
            
            int result = Sum(5, 5);
            Assert.True(result == 25, "Detour not called!");
        }
        
        [Fact]
        public void DetourMethodGenericTest()
        {
            if (OSHelper.IsHardenedRuntime)
                return;
            
            Person person = GenericMethod(new Person());
            Assert.True(person != null, "Detour not called!");
        }

        [Fact]
        public void SimpleDetourMethodStaticTest()
        {
            if (OSHelper.IsHardenedRuntime)
                return;
            
            SimpleMethodStatic();
            Assert.True(Trace.Contains(nameof(SimpleMethodDetourStatic)), "Detour static not called!");
        }

        [Fact]
        public void DetourMethodStaticParametersTest()
        {
            if (OSHelper.IsHardenedRuntime)
                return;
            
            int result = SumStatic(10, 10);
            Assert.True(result == 100, "Detour static not called!");
        }
        
        [Fact]
        public void DetourMethodGenericStaticTest()
        {
            if (OSHelper.IsHardenedRuntime)
                return;
            
            Person person = GenericMethodStatic(new Person());
            Assert.True(person != null, "Detour not called!");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void SimpleMethod()
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void SimpleMethodDetour() => AddMethodCalled();

        [MethodImpl(MethodImplOptions.NoInlining)]
        public int Sum(int n1, int n2) => n1 + n2;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public int Mul(int n1, int n2) => n1 * n2;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public T GenericMethod<T>(T obj) => default;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public T GenericMethodDetour<T>(T obj) => obj;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static int SumStatic(int n1, int n2) => n1 + n2;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static int MulStatic(int n1, int n2) => n1 * n2;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void SimpleMethodStatic()
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void SimpleMethodDetourStatic() => AddMethodCalled();

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static T GenericMethodStatic<T>(T obj) => default;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static T GenericMethodStaticDetour<T>(T obj) => obj;

        private static void AddMethodCalled([CallerMemberName] string detourMethod = "")
        {
            lock (Lock)
            {
                Trace.Add(detourMethod);
            }
        }

        private static void MethodResolver(MethodContext context)
        {
            if (context.Method == Utils.GetMethod<DetourMethodTest>(nameof(SimpleMethod)))
            {
                MethodInfo detour = Utils.GetMethod<DetourMethodTest>(nameof(SimpleMethodDetour));
                context.ResolveDetour(detour);
            }
            else if (context.Method == Utils.GetMethod<DetourMethodTest>(nameof(Sum)))
            {
                MethodInfo detour = Utils.GetMethod<DetourMethodTest>(nameof(Mul));
                context.ResolveDetour(detour);
            }
            else if (context.Method == Utils.GetMethod<DetourMethodTest>(nameof(SimpleMethodStatic)))
            {
                MethodInfo detour = Utils.GetMethod<DetourMethodTest>(nameof(SimpleMethodDetourStatic));
                context.ResolveDetour(detour);
            }
            else if (context.Method == Utils.GetMethod<DetourMethodTest>(nameof(SumStatic)))
            {
                MethodInfo detour = Utils.GetMethod<DetourMethodTest>(nameof(MulStatic));
                context.ResolveDetour(detour);
            }
            else if (context.Method.Name == nameof(GenericMethod))
            {
                MethodInfo detour = Utils.GetMethod<DetourMethodTest>(nameof(GenericMethodDetour));
                detour = detour.MakeGenericMethod(context.Method.GetGenericArguments());
                context.ResolveDetour(detour);
            }
            else if (context.Method.Name == nameof(GenericMethodStatic))
            {
                MethodInfo detour = Utils.GetMethod<DetourMethodTest>(nameof(GenericMethodStaticDetour));
                detour = detour.MakeGenericMethod(context.Method.GetGenericArguments());
                context.ResolveDetour(detour);
            }
        }
    }
}