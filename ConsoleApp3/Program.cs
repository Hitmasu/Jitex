using Jitex;
using System;
using System.Reflection;

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

            // MethodBase.GetMethodFromHandle()
            JitexManager.AddInterceptor(async (context) => { });

            //Console.WriteLine(typeof(MyClass<Program>).TypeHandle.Value.ToString("X"));
            //var lp = MyClass<Program>.MethodGeneric();
            var lp = MethodGeneric<Program>();
            Console.WriteLine(lp);
        }

        public static T MethodGeneric<T>()
        {
            return default;
        }
    }

    class MyClass
    {
        public string Name { get; set; }
        public static T MethodGeneric<T>()
        {
            return default;
        }

        internal class Abc
        {
                
        }
    }
}