using System;
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

        public ValueTask<int> Teste()
        {
            Console.WriteLine(Name);
            Console.WriteLine(Idade);
            return new ValueTask<int>(10);
        }
    }

    class Program
    {
        private static Point point = new Point(1, 2);

        static async Task Main()
        {
            Person p = new() {Idade = 999, Name = "Person name"};
            JitexManager.AddMethodResolver(MethodResolver);
            JitexManager.AddInterceptor(InteceptorCallAsync);

            //DynamicMethod dm = new DynamicMethod("Jitex", typeof(ValueTask<int>), Type.EmptyTypes);
            //ILGenerator generator = dm.GetILGenerator();

            //generator.Emit(OpCodes.Ldc_I4_8);
            //generator.Emit(OpCodes.Newobj,typeof(ValueTask<int>).GetConstructor(new[] { typeof(int) }));
            //generator.Emit(OpCodes.Ret);

            //ValueTask<int> ap = (ValueTask<int>) dm.Invoke(null,null);

            //var number = await p.Teste();
            Program asq = new();
            var number = await asq.A();
            var num2 = MarshalHelper.PreserveValueTask(0xFF);
            GC.KeepAlive(number);
            Console.WriteLine(number);
        }

        private static async ValueTask InteceptorCallAsync(CallContext context)
        {
            Console.WriteLine("Method intercepted");
        }

        public async ValueTask<int> A()
        {
            return 190;
        }

        private static void MethodResolver(MethodContext context)
        {
            if (context.Method.Name == nameof(Person.Teste) || context.Method.Name == "A")
                context.InterceptCall();
        }
    }
}