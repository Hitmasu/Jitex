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
                MethodInfo mi = (MethodInfo)context.Method;
                object newInstance = Activator.CreateInstance(mi.ReturnType);

                PropertyInfo propertyInfo = mi.ReturnType.GetProperty("Name");
                propertyInfo.SetValue(newInstance,"Stivi");

                context.ReturnValue = newInstance;
            });

            var lp = MyClass<IEnumerable<Program>>.MethodGeneric();
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