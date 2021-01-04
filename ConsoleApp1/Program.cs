using System;
using System.Runtime.CompilerServices;
using Jitex;
using Jitex.Intercept;
using Jitex.JIT.Context;

namespace ConsoleApp1
{
    class ABC { }

    class Program
    {
        static void Main()
        {
            JitexManager.AddMethodResolver(MethodResolver);
            JitexManager.AddInterceptor(Interceptor);
            JitexManager.AddTokenResolver(TokenResolver);
            
            Sum<int>(1, 1);

            Console.ReadKey();
        }

        private static void TokenResolver(TokenContext context)
        {
        }

        private static void Interceptor(CallContext context)
        {
            context.Continue();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static int Sum<T>(int n1, int n2)
        {
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
                Console.WriteLine(context.Method.MethodHandle.Value.ToString("X"));
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