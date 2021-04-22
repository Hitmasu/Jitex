using System.Diagnostics;
using System.Drawing;
using System.Threading.Tasks;
using Jitex;
using Jitex.Intercept;
using Jitex.JIT.Context;
using Console = System.Console;

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
            JitexManager.AddMethodResolver(MethodResolver);
            JitexManager.AddInterceptor(InteceptorCallAsync);
            
            int result = Teste(5, 5);
        }

        private static int Teste(int a, int b) => a + b;

        private static async ValueTask InteceptorCallAsync(CallContext context)
        {
            Console.WriteLine("Method intercepted");
        }

        private static void MethodResolver(MethodContext context)
        {
            if (context.Method.Name == nameof(Person.Teste) || context.Method.Name == "A")
                context.InterceptCall();
        }
    }
}