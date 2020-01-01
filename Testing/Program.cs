using Jitex.JIT;
using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Testing
{
    class Program
    {
        static void Main()
        {
            ManagedJit jit = ManagedJit.GetInstance();
            jit.OnPreCompile = OnPreCompile;
            int resultado = Somar(5, 5);
            Console.WriteLine(resultado);
            Console.ReadKey();
        }

        private static ReplaceInfo OnPreCompile(MethodBase methodToCompile)
        {
            MethodInfo methodMult = typeof(Program).GetMethod(nameof(Multiplicar));
            //MethodInfo methodSoma = typeof(Program).GetMethod(nameof(Nothing));
            MethodInfo methodC = typeof(Program).GetMethod(nameof(Somar));

            if (
                //methodToCompile.MetadataToken == methodSoma.MetadataToken
                //||
                methodToCompile.MetadataToken == methodC.MetadataToken
                )
            {
                byte[] asm = {
                    0x01, 0xD1, 0x89, 0xC8, 0xFF, 0xC0, 0xC3
                };

                //byte[] asm = methodSoma.GetMethodBody().GetILAsByteArray();

                return new ReplaceInfo(ReplaceInfo.ReplaceMode.ASM, asm);
            }

            return null;
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static int Somar(int num1, int num2)
        {
            return num1 + num2;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static int Multiplicar(int num1, int num2)
        {
            return num1 * num2;
        }
    }
}