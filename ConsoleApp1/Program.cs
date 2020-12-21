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
            A a = new A();
            a.MethodTesteInstance(static i =>
            {
                return 10;
            });

            Console.ReadKey();
        }

        public static void ShowTeste(long number)
        {
            Console.WriteLine(number.ToString("X"));
        }

        private static void MethodResolver(MethodContext context)
        {
            if (context.Method.Name == "MethodTesteInstance")
            {
                
            }
        }
    }

    public class A
    {
        public void MethodTesteInstance(int number)
        {
            Console.WriteLine("instance called");
        }
        
        public void MethodTesteInstance(Func<int,int> a) 
        {
            Console.WriteLine("instance called");
        }
    }
}