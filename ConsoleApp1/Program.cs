using System;
using Jitex;
using Jitex.JIT.Context;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            JitexManager.AddMethodResolver(MethodResolver);
            ShowTeste();
            ShowMe<int>();
            Console.ReadKey();
        }

        public static void ShowTeste()
        {
            ShowMe<Program>();
        }

        private static void MethodResolver(MethodContext context)
        {
            if (context.Method.Name == "ShowMe")
            {
                var methodToDetour = typeof(Program).GetMethod("Hook").MakeGenericMethod(typeof(int));
                context.DetourMethod(methodToDetour);
            }
        }

        public static A ShowMe<A>()
        {
            return default;
        }

        public static P Hook<P>()
        {
            Console.WriteLine("Hooked");
            return default;
        }
    }
}