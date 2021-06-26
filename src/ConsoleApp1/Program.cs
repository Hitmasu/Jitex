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

            var sumMethod = typeof(T<Program>).GetMethod("Sum", BindingFlags.Instance | BindingFlags.Public);
            var methodHandle = sumMethod.MethodHandle.Value;
            var pinter = sumMethod.MethodHandle.GetFunctionPointer();

            T<Program> classp = new T<Program>();
            classp.Sum();

            //Debug.WriteLine("Pré compiled: " + methodHandle.ToString("X") + "|" + pinter.ToString("X"));
            //Debugger.Break();
            //Sum();
            //Debug.WriteLine("After compiled: " + methodHandle.ToString("X") + "|" + pinter.ToString("X"));
            //Debugger.Break();
            MethodHelper.ForceRecompile(sumMethod);
            //Debug.WriteLine("After recompile: " + methodHandle.ToString("X") + "|" + pinter.ToString("X"));
            //Debugger.Break();
            //Sum();
            classp.Sum();
            Console.WriteLine("Finished!");
        }

        public static void Sum() { }
    }

    class T<U>
    {
        public void Sum() { }
    }
}
