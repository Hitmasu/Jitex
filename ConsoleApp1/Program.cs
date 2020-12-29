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

            int result = Sum<int>(1, 1);
            Console.WriteLine(result);
            Console.WriteLine(Test());
            Console.ReadKey();
        }

        public static int Test()
        {
            return Sum<int>(1, 1);
        }

        public static int Sum<T>(int n1, int n2)
        {
            return n1 + n2;
        }
        public static int Mul<T>(int n1, int n2)
        {
            return n1 * n2;
        }

        private static void MethodResolver(MethodContext context)
        {
            if (context.Method.Name == "Sum")
            {
                var m = typeof(Program).GetMethod(nameof(Mul)).MakeGenericMethod(context.Method.GetGenericArguments());
                context.ResolveDetour(m);
            }
        }
    }
}
