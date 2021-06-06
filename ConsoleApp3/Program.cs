using Jitex;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Jitex.Tests.Intercept;

namespace ConsoleApp3
{
    class Program
    {
        public string Name { get; set; }

        public Program()
        {
            Name = "Asdasdasd";
        }

        static async Task Main(string[] args)
        {
            JitexManager.AddMethodResolver((context) =>
            {
                if (context.Method.Name == "SimpleCallValueTaskAsync")
                    context.InterceptCall();
            });

            JitexManager.AddInterceptor(async (context) =>
            {
            });

            await new Teste().Caller();
        }
    }

    public class Teste
    {
        public async ValueTask Caller()
        {
            await SimpleCallValueTaskAsync();
        }

        public static async ValueTask SimpleCallValueTaskAsync()
        {
            Console.WriteLine("Called");
        }
    }

    class MyClass<T>
    {
        public string Name { get; set; }
        public static Abc MethodGeneric()
        {
            return new() { Name = "Flavio" };
        }

        internal class Abc
        {
            public string Name { get; set; }
        }
    }
}