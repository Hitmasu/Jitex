using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Jitex;
using Jitex.Intercept;
using Jitex.JIT.Context;
using Jitex.Utils;

namespace ConsoleApp1
{

    public class InterceptPerson
    {
        private string _name;
        public int Age { get; set; }

        public InterceptPerson()
        {

        }
        public InterceptPerson(string name)
        {
            _name = name;
        }

        public ref string GetName()
        {
            return ref _name;
        }
    }

    class Program
    {
        static async Task Main()
        {
            JitexManager.AddMethodResolver(MethodResolver);
            JitexManager.AddInterceptor(InterceptorAsyncCall);
            await Teste(10).ConfigureAwait(false);
            Debugger.Break();
        }

        private static async ValueTask InterceptorAsyncCall(CallContext context)
        {
            await context.ContinueAsync();
            Debugger.Break();
        }

        public static async Task<object> Teste(int a)
        {
            return Task.FromResult(new object());
        }

        private static void MethodResolver(MethodContext context)
        {
            if (context.Method.Name == nameof(Teste))
                context.InterceptCall();
        }
    }
}