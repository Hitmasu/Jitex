using System;
using System.Reflection.Emit;
using Jitex.Builder;
using Jitex.JIT;

namespace InjectMSIL
{
    class Program
    {
        static void Main(string[] args)
        {
            ManagedJit jit = ManagedJit.GetInstance();

            //Custom resolver
            jit.AddCompileResolver(CompileResolver);

            int result = SimpleSum(5, 5);
            Console.WriteLine(result); //output is 25
        }

        /// <summary>
        ///     Simple method to override.
        /// </summary>
        static int SimpleSum(int num1, int num2)
        {
            return num1 + num2;
        }

        private static void CompileResolver(CompileContext context)
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