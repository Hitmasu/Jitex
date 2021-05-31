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

            var result = new Program().MethodGeneric<int, int>(10, "Julia");

            Console.WriteLine(result);
        }

        T MethodGeneric<T,U>(int n1, string s) where T : new()
        {
            Console.WriteLine(n1);
            Console.WriteLine(s);
            Console.WriteLine(typeof(T).Name);
            Console.WriteLine(typeof(U).Name);
            return new T();
        }
    }
}
