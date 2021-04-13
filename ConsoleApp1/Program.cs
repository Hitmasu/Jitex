using System;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Jitex;
using Jitex.Intercept;
using Jitex.JIT.Context;
using Jitex.Runtime;
using Jitex.Utils;
using Console = System.Console;
using IntPtr = System.IntPtr;

namespace ConsoleApp1
{
    public class Person
    {
        public string Name { get; set; }
        public int Idade { get; set; }

        private static Point point = new Point(3, -1);

        public ValueTask<int> Teste(Point p, Point s)
        {
            Console.WriteLine(Name);
            Console.WriteLine(Idade);
            return new ValueTask<int>(10);
        }
    }

    class Program
    {
        private static Point point = new Point(1, 2);

        static async Task Main()
        {
            Person p = new() {Idade = 999, Name = "Person name"};
            JitexManager.AddMethodResolver(MethodResolver);
            JitexManager.AddInterceptor(InteceptorCallAsync);
            
            var number = await p.Teste(point, point);

            GC.KeepAlive(number);
            Console.WriteLine(number);
        }

        private static async ValueTask InteceptorCallAsync(CallContext context)
        {
            Console.WriteLine("Method intercepted");
        }

        public static async ValueTask<int> A()
        {
            return await new ValueTask<int>(190);
        }

        private static void MethodResolver(MethodContext context)
        {
            if (context.Method.Name == nameof(Person.Teste))
                context.InterceptCall();
        }
    }
}