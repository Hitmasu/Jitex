using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Jitex;
using Jitex.Intercept;
using Jitex.JIT.Context;
using Jitex.Utils;

namespace ConsoleApp1
{
    class Program
    {
        static async Task Main()
        {
            JitexManager.AddMethodResolver(MethodResolver);
            JitexManager.AddInterceptor(InterceptorAsyncCall);
            await Teste(10);
        }

        private static async ValueTask InterceptorAsyncCall(CallContext context)
        {
            context.Parameters!.SetParameterValue(0,999);
            await context.ContinueAsync();
        }

        public static async Task Teste(int a)
        {
            Console.WriteLine(a);
        }

        private static void MethodResolver(MethodContext context)
        {
            if (context.Method.Name == nameof(Teste))
                context.InterceptCall();
        }
    }
}