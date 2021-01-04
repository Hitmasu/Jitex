using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Jitex;
using Jitex.Intercept;
using Jitex.JIT.Context;

namespace ConsoleApp1
{
    class ABC{}

    class Program
    {
        private static MethodBase m = null;
        static void Main()
        {
            JitexManager.AddMethodResolver(MethodResolver);
            JitexManager.AddInterceptor(Interceptor);

            int result = Sum<Program>(1, 1);
            Console.WriteLine(result);
            result = Sum<Program>(1, 1);
            Console.WriteLine(result);
            JitexManager.EnableIntercept(m);
            result = Sum<Program>(1, 1);
            Console.WriteLine(result);

            Console.ReadKey();
        }

        private static void Caller(int n1)
        {
        }

        private static void Interceptor(CallContext context)
        {
            context.Continue();
            context.ReturnValue = 999;
            m = context.Method;
            context.DisableIntercept();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static int Sum<T>(int n1, int n2)
        {
            Console.WriteLine(typeof(T));
            return Sum2();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]

        public static int Sum2()
        {
            return 19;
        }

        private static void MethodResolver(MethodContext context)
        {
            if (context.Method.Name == "Sum")
                context.InterceptCall();
        }
    }

    class Gen<T>
    {
        public void Get()
        {
            Console.WriteLine(nameof(T));
        }
    }
}