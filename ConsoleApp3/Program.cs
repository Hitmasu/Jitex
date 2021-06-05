using Jitex;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;

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

            IEnumerable<Program> abc = new[] { new Program() };

            JitexManager.AddInterceptor(async (context) =>
            {
                Type genericArgument = context.Method.DeclaringType.GetGenericArguments()[0];

                if (genericArgument == typeof(IEnumerable<Program>))
                    context.ReturnValue = abc;
                else if (genericArgument == typeof(int))
                    context.ReturnValue = 40028922;
            });

            var lista = MyClass<IEnumerable<Program>>.MethodGeneric();
            Console.WriteLine(lista.Count());

            var numero = MyClass<int>.MethodGeneric();
            Console.WriteLine(numero);
        }
    }

    class MyClass<T>
    {
        public string Name { get; set; }
        public static T MethodGeneric()
        {
            Console.WriteLine(typeof(T).Name);
            return default;
        }

        internal class Abc
        {

        }
    }
}