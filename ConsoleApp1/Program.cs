using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Jitex;
using Jitex.Intercept;
using Jitex.JIT.Context;
using Jitex.Utils;

namespace ConsoleApp1
{
    struct A
    {
        public int a;
        public int b;
        public int c;
        public int d;

        public A(int a, int b)
        {
            this.a = a;
            this.b = b;
            c = 10;
            d = 0;
        }
    }

    class Person
    {
        public string Name { get; set; }
    }

    class Program
    {
        static async Task Main()
        {
            JitexManager.AddMethodResolver(MethodResolver);
            JitexManager.AddInterceptor(InterceptorAsyncCall);
            var apx = await Teste();
            Debugger.Break();
        }

        private static async ValueTask InterceptorAsyncCall(CallContext context)
        {
            
        }

        public static async Task<Person> Teste()
        {
            return ;
        }

        private static void MethodResolver(MethodContext context)
        {
            if (context.Method.Name == nameof(Teste))
                context.InterceptCall();
        }
    }
}