using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Security.Cryptography;
using Jitex.JIT.Context;
using Xunit;
using static Jitex.Tests.Utils;
using MethodBody = Jitex.Builder.Method.MethodBody;

namespace Jitex.Tests
{
    public class ReplaceILTests
    {
        private static readonly MethodInfo AssertTrue;

        static ReplaceILTests()
        {
            AssertTrue = typeof(Assert).GetMethod("True", new[] { typeof(bool) });
        }

        public ReplaceILTests()
        {
            Jitex.AddMethodResolver(MethodResolver);
            Jitex.AddTokenResolver(TokenResolver);
        }

        [Fact]
        public void BodyEmpty()
        {
            Assert.True(false, "IL not replaced");
        }

        [Fact]
        public void LocalVariable()
        {
            int number = short.MaxValue;
            Assert.True(number == ushort.MaxValue, "Variable not inserted!");
        }

        private void TokenResolver(TokenContext context)
        {
            if (context.Source != null)
            {
                if (context.Source.Name == "BodyEmpty" || context.Source.Name == "LocalVariable")
                {
                    if (context.MetadataToken >> 24 == 6)
                    {
                        context.ResolveMethod(AssertTrue);
                    }
                }
            }
        }

        private void MethodResolver(MethodContext context)
        {
            if (context.Method == GetMethod<ReplaceILTests>(nameof(BodyEmpty)))
            {
                byte[] assertToken = BitConverter.GetBytes(AssertTrue.MetadataToken);

                List<byte> il = new List<byte>
                {
                    (byte) OpCodes.Ldc_I4_1.Value,
                    (byte) OpCodes.Call.Value
                };

                il.AddRange(assertToken);
                il.Add((byte)OpCodes.Ret.Value);

                context.ResolveIL(il);
            }
            else if (context.Method == GetMethod<ReplaceILTests>(nameof(LocalVariable)))
            {
                byte[] assertToken = BitConverter.GetBytes(AssertTrue.MetadataToken);
                byte[] ceqInstruction = BitConverter.GetBytes(OpCodes.Ceq.Value).Reverse().ToArray();

                List<byte> il = new List<byte>
                {
                    (byte) OpCodes.Ldc_I4.Value, 0xFF, 0x7F, 0x00, 0x00,//32767
                    (byte) OpCodes.Stloc_0.Value,

                    (byte) OpCodes.Ldc_I4.Value, 0xFF, 0x7F, 0x00, 0x00, //32767
                    (byte) OpCodes.Stloc_1.Value,

                    (byte) OpCodes.Ldloc_0.Value,
                    (byte) OpCodes.Ldloc_1.Value,
                    (byte) OpCodes.Add.Value,
                    (byte) OpCodes.Ldc_I4_1.Value,
                    (byte) OpCodes.Add.Value,
                    (byte) OpCodes.Ldc_I4.Value,
                    0xFF, 0xFF, 0x00, 0x00, //65535
                };

                il.AddRange(ceqInstruction);
                il.Add((byte)OpCodes.Call.Value);
                il.AddRange(assertToken);
                il.Add((byte)OpCodes.Ret.Value);

                MethodBody methodBody = new MethodBody(il, context.Method.Module, typeof(int), typeof(int));
                context.ResolveBody(methodBody);
            }
        }
    }
}
