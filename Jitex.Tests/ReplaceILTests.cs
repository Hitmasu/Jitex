using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Jitex.JIT.Context;
using Xunit;

namespace Jitex.Tests
{
    public class ReplaceILTests
    {
        public ReplaceILTests()
        {
            Jitex.AddMethodResolver(MethodResolver);
        }

        [Fact]
        public void BodyEmpty()
        {
            Assert.True(false, "IL not replaced");
        }

        private void MethodResolver(MethodContext context)
        {
            if (context.Method.Name == nameof(BodyEmpty))
            {
                MethodInfo assertMethod = typeof(Assert).GetMethod("True", new[] {typeof(bool), typeof(string)});
                byte[] metadataToken = BitConverter.GetBytes(assertMethod.MetadataToken);
                metadataToken[3] = 0x0A; //fix reference;

                List<byte> il = new List<byte>
                {
                    (byte) OpCodes.Ldc_I4_1.Value,
                    (byte) OpCodes.Ldstr.Value
                };

                il.AddRange(new byte[] { 0x01, 0x00, 0x00, 0x70 });
                il.Add((byte)OpCodes.Call.Value);
                il.AddRange(metadataToken);
                il.Add((byte) OpCodes.Ret.Value);

                context.ResolveIL(il);
            }
        }
    }
}
