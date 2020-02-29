using Jitex.JIT;
using System;
using System.Collections.Generic;
using System.Reflection;
using Jitex.PE;
using LocalVariableInfo = Jitex.Builder.LocalVariableInfo;
using MethodBody = Jitex.Builder.MethodBody;

namespace Testing
{
    class Program
    {
        private static MetadataInfo _metadata;
        private static void Main()
        {
            ManagedJit managedJit = ManagedJit.GetInstance();
            managedJit.OnPreCompile = OnPreCompile;
            var result = Somar();
            Console.WriteLine(result);
            Console.ReadKey();
        }

        private static ReplaceInfo OnPreCompile(MethodBase method)
        {
            MethodInfo methodSomar = typeof(Program).GetMethod(nameof(Program.Somar));

            if (methodSomar.MetadataToken == method.MetadataToken)
            {
                if (_metadata == null)
                    _metadata = new MetadataInfo(Assembly.GetExecutingAssembly());

                byte[] il =
                {
                    0x1F,0x64,0xA,0x22,0x0,0x0,0x20,0x41,0xB,0x23,0x0,0x0,0x0,0x0,0x0,0x0,0x34,0x40,0xC,0x73,
                    0x0,0x0,0x0,0x0, //.ctor Random
                    0xD,0x6,0x6B,0x7,0x58,0x6C,0x8,0x58,0x9,0x16,0x6,0x6F,0x26,0x0,0x0,0xA,0x6C,0x58,0x2A
                };

                var ctorRandom = typeof(Random)
                    .GetConstructor(BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Any, Type.EmptyTypes, null);

                int memberRefToken = _metadata.GetMemberRefToken(ctorRandom.MetadataToken);
                byte[] byteRefToken = BitConverter.GetBytes(memberRefToken);

                Array.Copy(byteRefToken, 0, il, 20, byteRefToken.Length);

                List<LocalVariableInfo> localVariables = new List<LocalVariableInfo>
                {
                    new LocalVariableInfo(typeof(int)),
                    new LocalVariableInfo(typeof(float)),
                    new LocalVariableInfo(typeof(double)),
                    new LocalVariableInfo(typeof(Random)),
                    new LocalVariableInfo(typeof(double)) //ret type
                };

                MethodBody body = new MethodBody(il, localVariables, typeof(Program).Module);
                return new ReplaceInfo(body);
            }

            return null;
        }

        public static double Somar()
        {
            return 10;
        }
    }
}