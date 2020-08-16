using System;
using Jitex.JIT;

namespace InjectNativeCode
{
    class Program
    {
        static void Main(string[] args)
        {
            ManagedJit jit = ManagedJit.GetInstance();
            jit.AddCompileResolver(CompileResolver);
            int result = SimpleSum(1, 7);
            Console.WriteLine(result);
        }

        static int SimpleSum(int num1, int num2)
        {
            return num1 + num2;
        }

        private static void CompileResolver(CompileContext context)
        {
            if (context.Method.Name == "SimpleSum")
            {
                //Replace with fatorial number:
                //int sum = num1+num2;
                //int fatorial = 1;
                //for(int i = 2; i <= sum; i++){
                //    fatorial *= i;
                //}
                //return fatorial;
                byte[] asm =
                {
                    0x01, 0xCA,                     //add    edx,ecx
                    0xB8, 0x01, 0x00, 0x00, 0x00,   //mov    eax,0x1
                    0xB9, 0x02, 0x00, 0x00, 0x00,   //mov    ecx,0x2
                    0x83, 0xFA, 0x02,               //cmp    edx,0x2
                    0x7C, 0x09,                     //jl
                    0x0F, 0xAF, 0xC1,               //imul   eax,ecx
                    0xFF, 0xC1,                     //inc    ecx
                    0x39, 0xD1,                     //cmp    ecx,edx
                    0x7E, 0xF7,                     //jle
                    0xC3                            //ret
                };
                context.ResolveByteCode(asm);
            }
        }
    }
}
