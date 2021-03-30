using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Jitex;
using Jitex.Intercept;
using Jitex.JIT.Context;
using Jitex.Utils;
using Console = System.Console;

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
        public int Idade { get; set; }

        public ValueTask Teste()
        {
            Console.WriteLine("Nome: " + Name);
            Console.WriteLine("Idade: " + Idade);
            return new ValueTask();
        }
    }

    class Program
    {
        private static Point point = new Point(1,2);

        // private static async ValueTask Teste()
        // {
        //     return;
        // }

        static async Task Main()
        {
            JitexManager.AddMethodResolver(MethodResolver);
            JitexManager.AddInterceptor(InteceptorCallAsync);

            Person p = new Person();
            p.Teste();
            // var lp = p.Teste();
            // await lp;
            await Task.Delay(-1);
        }

        private static async ValueTask InteceptorCallAsync(CallContext context)
        {
            context.DisableIntercept();
            //context.Continue();
            Console.WriteLine("Method intercepted");
        }

        private static void MethodResolver(MethodContext context)
        {
            if (context.Method.Name == nameof(Person.Teste))
                context.InterceptCall();
        }
    }
}