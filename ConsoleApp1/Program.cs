using System;
using System.Runtime.CompilerServices;
using Jitex;
using Jitex.Intercept;
using Jitex.JIT.Context;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            JitexManager.AddMethodResolver(MethodResolver);
            JitexManager.AddInterceptor(Interceptor);

            ABC abc = new ABC();
            int sum = Sum();

            Console.WriteLine(sum);
            Console.ReadKey();
        }

        private static void Interceptor(CallContext context)
        {
            int originalResult = context.Continue<int>();
            context.ReturnValue = originalResult*20;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static int Sum() => 20;

        private static void MethodResolver(MethodContext context)
        {
            if (context.Method.Name.Contains("ctor") && context.Method.DeclaringType == typeof(ABC))
                context.InterceptCall();
        }
    }

    class ABC
    {
        public int Test(int n1, int n2)
        {
            return n1 + n2;
        }
    }
}