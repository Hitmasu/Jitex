using System;
using System.IO;
using System.Runtime.CompilerServices;
using Iced.Intel;
using Jitex;
using Jitex.JIT.Context;
using static Iced.Intel.AssemblerRegisters;

namespace InjectNativeCode
{
    class Program
    {
        static void Main(string[] args)
        {
            JitexManager.AddMethodResolver(MethodResolver);
            int result = SimpleSum(1, 7);
            Console.WriteLine(result);
            Console.ReadKey();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int SimpleSum(int num1, int num2)
        {
            return num1 + num2;
        }

        private static void MethodResolver(MethodContext context)
        {
            if (context.Method.Name == "SimpleSum")
            {
                Assembler assembler = new Assembler(64);

                //Replace with fatorial number:
                //int sum = num1+num2;
                //int fatorial = 1;
                //for(int i = 2; i <= sum; i++){
                //    fatorial *= i;
                //}
                //return fatorial;
                assembler.add(edx, ecx);
                assembler.mov(eax, 1);
                assembler.mov(ecx, 2);
                assembler.cmp(edx, 0x02);

                assembler.jl(assembler.@F);
                assembler.AnonymousLabel();
                assembler.imul(eax, ecx);
                assembler.inc(ecx);
                assembler.cmp(ecx, edx);
                assembler.jle(assembler.@B);
                assembler.AnonymousLabel();
                assembler.ret();

                using MemoryStream ms = new MemoryStream();
                assembler.Assemble(new StreamCodeWriter(ms), 0);

                byte[] asm = ms.ToArray();

                context.ResolveNative(asm);
            }
        }
    }
}