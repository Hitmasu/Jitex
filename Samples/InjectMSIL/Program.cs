using System;
using System.Reflection.Emit;
using Jitex;
using Jitex.Builder.Method;
using Jitex.JIT.Context;

namespace InjectMSIL
{
    class Program
    {
        static void Main(string[] args)
        {
            JitexManager.AddMethodResolver(MethodResolver);

            int result = SimpleSum(5, 5);
            Console.WriteLine(result); //output is 25
            Console.ReadKey();
        }

        /// <summary>
        ///     Simple method to override.
        /// </summary>
        static int SimpleSum(int num1, int num2)
        {
            return num1 + num2;
        }

        private static void MethodResolver(MethodContext context)
        {
            //Verify with method to be compile is our method who we want modify.
            if (context.Method.Name == "SimpleSum")
            {
                //num1 * num2
                byte[] newIL =
                {
                    (byte) OpCodes.Ldarg_0.Value, //parameter num1
                    (byte) OpCodes.Ldarg_1.Value, //parameter num2
                    (byte) OpCodes.Mul.Value,
                    (byte) OpCodes.Ret.Value
                };

                MethodBody body = new MethodBody(newIL, context.Method.Module);
                context.ResolveBody(body);
            }
        }
    }
}