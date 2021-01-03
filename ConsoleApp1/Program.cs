using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Jitex;
using Jitex.Intercept;
using Jitex.JIT.Context;

namespace ConsoleApp1
{
    class ABC{}

    class Program
    {
        static void Main()
        {
            JitexManager.AddMethodResolver(MethodResolver);
            JitexManager.AddInterceptor(Interceptor);

            Program p = new Program();

            int sum = p.Sum<Program>(2, 5);
            Console.WriteLine(sum);

            //sum = p.Sum<int>(2, 5);
            //Console.WriteLine(sum);

            sum = p.Sum<ABC>(2, 5);
            Console.WriteLine(sum);

            Debugger.Break();

            Console.ReadKey();
        }

        private static void Interceptor(CallContext context)
        {
            Random rnd = new Random();
            context.ReturnValue = rnd.Next();
            JitexManager.DisableIntercept(context.Method);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public int Sum<T>(int n1, int n2)
        {
            Console.WriteLine(typeof(T));
            return n1 + n2;
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