using System;
using System.Linq;
using Jitex;
using Jitex.JIT.Context;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            JitexManager.AddMethodResolver(MethodResolver);
            ShowMe<int>();
            ShowMe<float>();
            Console.ReadKey();
        }

        private static void MethodResolver(MethodContext context)
        {
            if (context.Method.Name != "ShowMe")
                return;
            
            // context.ResolveDetour();
        }

        public static void ShowMe<T>()
        {
            Console.WriteLine(typeof(T).Name);
        }
    }
}