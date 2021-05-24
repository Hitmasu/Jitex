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
    class Result
    {
        public int Value { get; set; }
    }

    class Program
    {
        static async Task Main(string[] args)
        {
            JitexManager.AddMethodResolver(MethodResolver);
            JitexManager.AddInterceptor(InterceptorCallAsync);
            var result = await new Program().Sum(1,1,null);
            
        }

        public async ValueTask<int> Sum(int n1, int n2,Result result)
        {
            Console.WriteLine(n1);
            Console.WriteLine(n2);
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
    }
}
