using System;
using System.Diagnostics;
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

        private static Point point = new Point(3, -1);

        public async ValueTask Teste(Point pointer)
        {
            unsafe
            {
                IntPtr valueAddr = *(IntPtr*)MarshalHelper.GetReferenceFromTypedReference(__makeref(pointer));
                Console.WriteLine(valueAddr.ToString("X"));
            }

            Console.WriteLine("Name: " + Name);
            Console.WriteLine("Age: " + Idade);
            Console.WriteLine("X: " + pointer.X);
            Console.WriteLine("Y: " + pointer.Y);
        }
    }

    class Program
    {
        private static Point point = new Point(1, 2);

        static async Task Main()
        {
            Person p = new Person { Idade = 999, Name = "Person name" };
            JitexManager.AddMethodResolver(MethodResolver);
            JitexManager.AddInterceptor(InteceptorCallAsync);

            await p.Teste(point);

            Console.ReadKey();
        }

        private static async ValueTask InteceptorCallAsync(CallContext context)
        {
            Console.WriteLine("Method intercepted");
        }

        private static void MethodResolver(MethodContext context)
        {
            if (context.Method.Name == nameof(Person.Teste))
                context.InterceptCall();
        }
    }
}