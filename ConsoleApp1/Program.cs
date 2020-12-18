using System;
using System.Diagnostics;
using System.Linq.Expressions;
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
            Console.ReadKey();
        }

        public static void ShowTeste()
        {
            Console.WriteLine("ABC");
        }

        private static void MethodResolver(MethodContext context)
        {
            if (context.Method.Name == "ShowTeste")
            {
                context.Detour<Action>(() =>
                {
                    Console.WriteLine("aspodk");
                });
            }
        }
    }
}