using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jitex;
using Jitex.Intercept;
using Jitex.JIT.Context;
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
        static async Task Main()
        {
            JitexManager.AddMethodResolver(MethodResolver);
            JitexManager.AddInterceptor(InteceptorCallAsync);

            Person p = new Person {Name = "Flávio", Idade = 99};
            await p.Teste().ConfigureAwait(false);

            await Task.Delay(-1);
        }

        private static async ValueTask InteceptorCallAsync(CallContext context)
        {
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