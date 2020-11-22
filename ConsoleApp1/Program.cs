using System;
using System.Diagnostics;
using System.Reflection;
using Jitex;
using Jitex.JIT.Context;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            JitexManager.AddMethodResolver(MethodResolver);
            JitexManager.AddTokenResolver(TokenResolver);
            ShowTeste();
            Console.ReadKey();
        }


        public static void ShowTeste()
        {
            ShowMe<int>();
        }

        private static void MethodResolver(MethodContext context)
        {
            if (context.Method.Name == "ShowMe")
            {
                var methodToDetour = typeof(Program).GetMethod("Hook").MakeGenericMethod(typeof(int));
                context.DetourMethod(methodToDetour);
            }
        }

        private static void TokenResolver(TokenContext context)
        {
            //if (context.Source?.Name == "ShowTeste")
                //Debugger.Break();
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