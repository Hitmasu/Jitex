using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Jitex.JIT.Context;
using Xunit;
using static Jitex.Tests.Utils;
using MethodBody = Jitex.Builder.Method.MethodBody;

namespace Jitex.Tests.Resolvers
{
    [Collection("Manager")]
    public class ResolveILTests
    {
        private static readonly MethodInfo AssertTrue;

        static ResolveILTests()
        {
            AssertTrue = typeof(Assert).GetMethod("True", new[] {typeof(bool)});
        }

        public ResolveILTests()
        {
            JitexManager.AddMethodResolver(MethodResolver);
            JitexManager.AddTokenResolver(TokenResolver);
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
            if (context.Module == GetType().Module && context.HasSource && context.Source!.DeclaringType == typeof(ResolveILTests))
            {
                if (context.MetadataToken == AssertTrue.MetadataToken)
                    context.ResolveMethod(AssertTrue);
            }
        }

        private void MethodResolver(MethodContext context)
        {
            if (context.Method == GetMethod<ResolveILTests>(nameof(BodyEmpty)))
            {
                byte[] assertToken = BitConverter.GetBytes(AssertTrue.MetadataToken);

                //{
                //  Assert.True(true);
                //}
                List<byte> il = new List<byte>
                {
                    (byte) OpCodes.Ldc_I4_1.Value,
                    (byte) OpCodes.Call.Value
                };

                il.AddRange(assertToken);
                il.Add((byte) OpCodes.Ret.Value);

                context.ResolveIL(il);
            }
            else if (context.Method == GetMethod<ResolveILTests>(nameof(LocalVariable)))
            {
                byte[] assertToken = BitConverter.GetBytes(AssertTrue.MetadataToken);
                byte[] ceqInstruction = BitConverter.GetBytes(OpCodes.Ceq.Value).Reverse().ToArray();

                //{
                //  short v1 = short.MaxValue;
                //  short v2 = short.MaxValue;
                //  Assert.True(v1+v2+1 == ushort.MaxValue);
                //}
                List<byte> il = new List<byte>
                {
                    (byte) OpCodes.Ldc_I4.Value, 0xFF, 0x7F, 0x00, 0x00, //32767
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
                il.Add((byte) OpCodes.Call.Value);
                il.AddRange(assertToken);
                il.Add((byte) OpCodes.Ret.Value);

                MethodBody methodBody = new MethodBody(il, context.Method.Module, typeof(int), typeof(int));
                context.ResolveBody(methodBody);
            }
        }
    }
}