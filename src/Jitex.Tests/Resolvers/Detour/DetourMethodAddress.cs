using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Jitex.JIT.Context;
using Jitex.Utils;
using Xunit;

namespace Jitex.Tests.Detour
{
    [Collection("Manager")]
    public class DetourMethodAddress
    {
        static DetourMethodAddress()
        {
            JitexManager.AddMethodResolver(MethodResolver);
        }

        [Fact]
        public void DetourAddressTest()
        {
            if (OSHelper.IsHardenedRuntime)
                return;

            int result = Sum(7, 7);
            Assert.True(result == 49, "Detour not called!");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public int Sum(int n1, int n2) => n1 + n2;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public int Mul(int n1, int n2) => n1 * n2;

        private static void MethodResolver(MethodContext context)
        {
            if (context.Method == Utils.GetMethod<DetourMethodAddress>(nameof(Sum)))
            {
                MethodInfo mulMethod = Utils.GetMethod<DetourMethodAddress>(nameof(Mul));
                RuntimeMethodHandle handle = mulMethod.MethodHandle;

                RuntimeHelpers.PrepareMethod(handle);

                IntPtr methodPointer = handle.GetFunctionPointer();

                byte jmp = Marshal.ReadByte(methodPointer);

                if (jmp == 0xE9)
                {
                    int jmpSize = Marshal.ReadInt32(methodPointer + 1);
                    methodPointer = new IntPtr(methodPointer.ToInt64() + jmpSize + 5);
                }

                context.ResolveDetour(methodPointer);
            }
        }
    }
}