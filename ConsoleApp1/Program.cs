using System.Diagnostics;
using System.Drawing;
using System.Threading.Tasks;
using Jitex;
using Jitex.Intercept;
using Jitex.JIT.Context;
using Console = System.Console;

namespace ConsoleApp1
{
    class Program
    {
        private static Point point = new Point(1, 2);

        static void Main()
        {
            JitexManager.AddMethodResolver(MethodResolver);
            JitexManager.AddInterceptor(InteceptorCallAsync);
            
            int result = SimpleSum(5, 5);
        }

        private static int SimpleSum(int a, int b) => a + b;

        private static async ValueTask InteceptorCallAsync(CallContext context)
        {
            //Get parameters passed to method
            int n1 = context.Parameters.GetParameterValue<int>(0);
            int n2 = context.Parameters.GetParameterValue<int>(1);
            
            //Set new parameters value to call
            context.Parameters.SetParameterValue(0,999);
            context.Parameters.SetParameterValue(1,1);

            //Set return value;
            context.ReturnValue = 50;
            
            //Prevent method original to be called
            context.ProceedCall = false;
            
            //Continue original call
            int result = await context.ContinueAsync<int>();
        }

        private static void MethodResolver(MethodContext context)
        {
            if (context.Method.Name == nameof(SimpleSum))
                context.InterceptCall();
        }
    }
}