using Jitex.JIT;
using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Testing
{
    class abc : Attribute
    {

    }

    class CustomAttribute : Attribute
    {

    }
    class Program
    {
        static void Main()
        {
            using (ManagedJit jit = ManagedJit.GetInstance())
            {
                jit.OnPreCompile = method => null;
                Somar<int>(1,1);
            }
            
            MethodInfo c = typeof(Program).GetMethod("HackedMethod");
            var attr = c.GetCustomAttributes();
            Console.ReadKey();
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static int Somar<T>(int num1, int num2)
        {
            Console.WriteLine(typeof(T));
            return num1 + num2;
        }

        [abc]
        [CustomAttribute]
        public static int HackedMethod(int num1, int num2)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("-------Hacked-------");
            Console.ForegroundColor = ConsoleColor.White;

            return CustomMethod();
        }

        private static int CustomMethod()
        {
            Console.WriteLine("Custom method called!");
            return Return();
        }

        private static int Return()
        {
            return 10 + 10 + 90;
        }
    }
}