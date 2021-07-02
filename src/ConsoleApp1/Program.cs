using Jitex;
using Jitex.Utils;
using System;
using System.Diagnostics;
using System.Reflection;


namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            JitexManager.AddMethodResolver((context) =>
            {
                if (context.Method.Name == "Sum")
                    Console.WriteLine("Compiled2");
            });
            //var sumMethod = typeof(Program).GetMethod("Sum");

            var sumMethod = typeof(T).GetMethod("Sum").MakeGenericMethod(typeof(int));
            var methodHandle = sumMethod.MethodHandle.Value;
            var pinter = sumMethod.MethodHandle.GetFunctionPointer();


            Debug.WriteLine("Pré compiled: " + sumMethod.MethodHandle.Value.ToString("X") + "|" + sumMethod.MethodHandle.GetFunctionPointer().ToString("X"));
            Debugger.Break();
            T.Sum<int>();
            Debug.WriteLine("After compiled: " + sumMethod.MethodHandle.Value.ToString("X") + "|" + sumMethod.MethodHandle.GetFunctionPointer().ToString("X"));
            Debugger.Break();
            MethodHelper.ForceRecompile(sumMethod);
            Debug.WriteLine("After write: " + sumMethod.MethodHandle.Value.ToString("X") + "|" + sumMethod.MethodHandle.GetFunctionPointer().ToString("X"));
            Debugger.Break();
            //Debug.WriteLine("After recompile: " + methodHandle.ToString("X") + "|" + pinter.ToString("X"));
            //Debugger.Break();
            //Sum();
            T.Sum<int>();
            Console.WriteLine("Finished!");
        }

        public static void Sum<T>() { }
    }

    class T
    {
        public static void Sum<T>() { }
    }
}
