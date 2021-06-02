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

            var lp = MyClass<Program>.MethodGeneric();
            Console.WriteLine(lp);
        }

        public static T MethodGeneric<T>()
        {
            return default;
        }
    }

    class MyClass<T> where T : new()
    {
        public string Name { get; set; }
        public static T MethodGeneric()
        {
            return new T();
        }

        internal class Abc
        {
                
        }
    }
}