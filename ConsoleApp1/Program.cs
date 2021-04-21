﻿using System;
using System.Drawing;
using System.Threading.Tasks;
using Jitex;
using Jitex.Intercept;
using Jitex.JIT.Context;
using Jitex.Utils;
using Console = System.Console;
using IntPtr = System.IntPtr;

namespace ConsoleApp1
{
    public class Person
    {
        public string Name { get; set; }
        public int Idade { get; set; }

        public ValueTask Teste(int n1, int n2)
        {
            Console.WriteLine(Name);
            Console.WriteLine(Idade);
            return ValueTask.CompletedTask;

        }
    }

    class Program
    {
        private static Point point = new Point(1, 2);

        static async Task Main()
        {
            Person p = new() { Idade = 999, Name = "Person name" };
            JitexManager.AddMethodResolver(MethodResolver);
            JitexManager.AddInterceptor(InteceptorCallAsync);

            //DynamicMethod dm = new DynamicMethod("Jitex", typeof(ValueTask<int>), Type.EmptyTypes);
            //ILGenerator generator = dm.GetILGenerator();

            //generator.Emit(OpCodes.Ldc_I4_8);
            //generator.Emit(OpCodes.Newobj,typeof(ValueTask<int>).GetConstructor(new[] { typeof(int) }));
            //generator.Emit(OpCodes.Ret);

            //ValueTask<int> ap = (ValueTask<int>) dm.Invoke(null,null);
            int result = await Teste(1,1);
        }

        private static async ValueTask InteceptorCallAsync(CallContext context)
        {
            Console.WriteLine("Method intercepted");
        }

        private static async ValueTask<int> Teste(int n1, int n2)
        {
            Console.WriteLine("Called");
            return await new ValueTask<int>(n1 + n2);
        }

        private static void MethodResolver(MethodContext context)
        {
            if (context.Method.Name == nameof(Person.Teste) || context.Method.Name == "A")
                context.InterceptCall();
        }
    }
}