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

        public ref Point Teste(Point pointer)
        {
            unsafe
            {
                IntPtr valueAddr = *(IntPtr*)MarshalHelper.GetReferenceFromTypedReference(__makeref(pointer));
                Console.WriteLine(valueAddr.ToString("X"));
            }

            Console.WriteLine("Nome: " + Name);
            Console.WriteLine("Idade: " + Idade);
            Console.WriteLine("X: " + pointer.X);
            Console.WriteLine("Y: " + pointer.Y);

            return ref point;
        }
    }

    class Program
    {
        private static Point point = new Point(1, 2);

        static void Main()
        {
            Person p = new Person { Idade = 24, Name = "Flávio" };
            IntPtr addr = MarshalHelper.GetReferenceFromTypedReference(__makeref(p));
            JitexManager.AddMethodResolver(MethodResolver);
            JitexManager.AddInterceptor(InteceptorCallAsync);

            unsafe
            {
                ref Point point2 = ref p.Teste(point);
                Debugger.Break();
            }

            Console.ReadKey();
        }

        private static async ValueTask InteceptorCallAsync(CallContext context)
        {
            context.ReturnValue = new Point(3, -1);
            Console.WriteLine("Method intercepted");
        }

        private static void MethodResolver(MethodContext context)
        {
            if (context.Method.Name == nameof(Person.Teste))
                context.InterceptCall();
        }
    }
}