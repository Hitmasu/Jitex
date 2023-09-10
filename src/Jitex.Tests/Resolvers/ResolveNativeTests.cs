using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Iced.Intel;
using Jitex.JIT.Context;
using Jitex.Utils;
using Xunit;
using static Jitex.Tests.Utils;
using static Iced.Intel.AssemblerRegisters;

namespace Jitex.Tests.Resolvers
{
    [Collection("Manager")]
    public class ResolveNativeTests
    {
        static ResolveNativeTests()
        {
            JitexManager.AddMethodResolver(MethodResolver);
        }

        [Fact]
        public void SmallAssemblyTest()
        {
            int n1 = 5;
            int n2 = 5;
            int expected = n1 * n2;
            int number = SimpleSum(n1, n2);
            Assert.True(number == expected, $"Native code not injected! {number}");
        }

        [Fact]
        public void LargeAssemblyTest()
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
            return n1 / n2;
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

                const int stackSize = 4;
                assembler.push(rbp);
                assembler.sub(rsp, stackSize);
                assembler.lea(rbp, __[rsp + stackSize]);
                assembler.mov(__dword_ptr[rbp - stackSize], 5);
                assembler.mov(rax, 5);
                assembler.imul(rax, __dword_ptr[rbp - stackSize]);
                assembler.lea(rsp, __[rbp]);
                assembler.pop(rbp);
                assembler.ret();

                using MemoryStream stream = new MemoryStream();
                assembler.Assemble(new StreamCodeWriter(stream), 0);
                context.ResolveNative(stream.ToArray());
            }
            else if (context.Method == GetMethod<ResolveNativeTests>(nameof(LargeSum)))
            {
                Assembler assembler = new Assembler(64);

                const int stackSize = 4;
                assembler.push(rbp);
                assembler.sub(rsp, stackSize);
                assembler.lea(rbp, __[rsp + stackSize]);
                assembler.mov(rdx, 10);
                assembler.mov(rax, 10);

                for (int i = 0; i < 999; i++)
                {
                    assembler.add(rax, rdx);
                }

                assembler.lea(rsp, __[rbp]);
                assembler.pop(rbp);
                assembler.ret();

                using MemoryStream stream = new MemoryStream();
                assembler.Assemble(new StreamCodeWriter(stream), 0);
                context.ResolveNative(stream.ToArray());
            }
        }
    }
}