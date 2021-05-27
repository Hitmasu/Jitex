using System;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
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
            var result = Sum<int>(1, 1, new Result {Value = 8520});
        }

        private static int Sum<T>(int n1, int n2, Result result)
        {
            Console.WriteLine(n1);
            Console.WriteLine(n2);
            Console.WriteLine(result.Value);
            // Console.WriteLine(typeof(T).Name);
            return 90;
        }

        private static async ValueTask InterceptorCallAsync(CallContext context)
        {
        }

        private static void MethodResolver(MethodContext context)
        {
            if (context.Method.Name == "Sum")
                context.InterceptCall();
        }
        
        private class Result
        {
            public int Value { get; set; }
        }
    }
}