using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Jitex;
using Jitex.Intercept;
using Jitex.JIT.Context;

namespace ConsoleApp1
{

    class Program
    {
        static async Task Main(string[] args)
        {
            JitexManager.AddMethodResolver(MethodResolver);
            JitexManager.AddInterceptor(InterceptorCallAsync);
            var a = Sum<Process>(10);
            Debugger.Break();
        }

        private static Result Sum<T>(int n1)
        {
            Console.WriteLine(n1);
            Console.WriteLine(typeof(T).Name);
            return new Result{Value = 999};
        }

        private static async ValueTask InterceptorCallAsync(CallContext context)
        {
        }

        private static void MethodResolver(MethodContext context)
        {
            if (context.Method.Name == "Sum")
                context.InterceptCall();
        }
         class Result
        {
            public int Value { get; set; }
        }
    }
}