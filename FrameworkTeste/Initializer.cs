using Jitex.JIT;
using Jitex.PE;
using System;
using System.Collections.Generic;
using System.Reflection;
using LocalVariableInfo = Jitex.Builder.LocalVariableInfo;
using MethodBody = Jitex.Builder.MethodBody;

namespace FrameworkTeste
{
    public class Initializer
    {
        private readonly ManagedJit _managedJit;
        private readonly Module _module;
        private readonly MetadataInfo _metadata;

        public Initializer(Module module)
        {
            _module = module;
            _metadata = new MetadataInfo(module.Assembly);
            _managedJit = ManagedJit.GetInstance();
            _managedJit.OnPreCompile = OnPreCompile;
        }

        private ReplaceInfo OnPreCompile(MethodBase method)
        {
            if (method.Name == "Somar")
            {
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

                MethodBody body = new MethodBody(il, localVariables, _module);
                return new ReplaceInfo(body);
            }

            return null;
        }
    }
}
