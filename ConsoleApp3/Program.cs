using Jitex;
using System;

namespace ConsoleApp3
{
    class Program
    {
        public string Name { get; set; }

        public Program()
        {
            Name = "Asdasdasd";
        }

        static void Main(string[] args)
        {
            JitexManager.AddMethodResolver((context) =>
            {
                if (context.Method.Name == "MethodGeneric")
                    context.InterceptCall();
            });

            JitexManager.AddInterceptor(async (context) => context.Parameters.SetParameterValue(1, "Flávio"));

            MyClass<int> teste = new MyClass<int>();
            var lp = teste.MethodGeneric<int>(20, "asd", new ());
            Console.WriteLine(lp);
        }
    }

    class MyClass<T> where T : new()
    {
        public Abc MethodGeneric<U>(int n1, string s, Abc t)
        {
            Console.WriteLine(n1);
            Console.WriteLine(s);
            Console.WriteLine(typeof(T).Name);
            Console.WriteLine(typeof(U).Name);
            return new Abc();
        }

        internal class Abc
        {
                
        }
    }
}