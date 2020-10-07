using System.IO;
using System.Runtime.CompilerServices;
using Iced.Intel;
using Jitex.JIT.Context;
using Xunit;

using static Jitex.Tests.Utils;
using static Iced.Intel.AssemblerRegisters;

namespace Jitex.Tests
{
    public class ResolveNativeTests
    {
        static ResolveNativeTests()
        {
            JitexManager.AddMethodResolver(MethodResolver);
        }

        [Fact]
        public void SmallAssembly()
        {
            int n1 = 5;
            int n2 = 5;
            int expected = n1 * n2;
            int number = SimpleSum(n1, n2);
            Assert.True(number == expected, "Native code not injected!");
        }


        [Fact]
        public void LargeAssembly()
        {
            int n1 = 10;
            int n2 = 1000;
            int expected = n1 * n2;
            int number = LargeSum(n1, n2);
            Assert.True(number == expected, $"Native code not injected! {number}");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public int SimpleSum(int n1, int n2)
        {
            return n1 + n2;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public int LargeSum(int n1, int n2)
        {
            return n1 + n2;
        }

        private static void MethodResolver(MethodContext context)
        {
            if (context.Method == GetMethod<ResolveNativeTests>(nameof(SimpleSum)))
            {
                Assembler assembler = new Assembler(64);

                assembler.mov(eax, edx);
                assembler.imul(eax, r8d);
                assembler.ret();

                context.ResolveNative(GetNativeCode(assembler));
            }
            else if (context.Method == GetMethod<ResolveNativeTests>(nameof(LargeSum)))
            {
                Assembler assembler = new Assembler(64);

                assembler.lea(eax,__dword_ptr[rdx]);

                for (int i = 0; i < 999; i++)
                {
                    assembler.add(eax, edx);
                }

                assembler.ret();

                context.ResolveNative(GetNativeCode(assembler));
            }
        }

        private static byte[] GetNativeCode(Assembler assembler)
        {
            using MemoryStream stream = new MemoryStream();
            assembler.Assemble(new StreamCodeWriter(stream), 0);

            return stream.ToArray();
        }
    }
}
