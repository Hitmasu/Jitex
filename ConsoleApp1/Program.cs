using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
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

    public struct B<T>
    {
        public T XYUZ;
        public T a;
        public int b;

        public B(T _a, int ba)
        {
            a = _a;
            XYUZ = a;
            b = 0x0A;
        }
    }

    public class Person
    {
        public string Name { get; set; }
    }

    class Program
    {
        static async Task Main()
        {
            JitexManager.AddMethodResolver(MethodResolver);
            JitexManager.AddInterceptor(InteceptorCallAsync);
            await Teste();
            Console.WriteLine("Called!");
            Console.ReadKey();
        }

        private static async ValueTask InteceptorCallAsync(CallContext context)
        {
            Console.WriteLine("Method intercepted");
        }

        public static async ValueTask Teste()
        {
            Console.WriteLine("method called");
        }

        private static void MethodResolver(MethodContext context)
        {
            if (context.Method.Name == nameof(Teste))
                context.InterceptCall();
        }
    }
}