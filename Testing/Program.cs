using Jitex.JIT;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Testing
{
    class Program
    {
        static void Main()
        {
            ManagedJit managedJit = ManagedJit.GetInstance();
            //managedJit.OnPreCompile = OnPreCompile;
            int resultado = Somar(1, 1);
            Console.Title = "Hook";
            Console.WriteLine(resultado);
            Console.ReadKey();
        }

        private static ReplaceInfo OnPreCompile(MethodBase method)
        {
            MethodInfo somarInfo = typeof(Program).GetMethod("Somar");

            if (somarInfo.MetadataToken == method.MetadataToken)
            {
                //num1 + num2 + 1 + 2 + 3 + 4 + 5 + 6 + ....

                //01 d1                   add    ecx,edx
                //83 c1 0a                add    ecx,0x1
                //83 c1 14                add    ecx,0x2
                //83 c1 1e                add    ecx,0x3
                //83 c1 28                add    ecx,0x4
                //...
                //89 c8                   mov    eax,ecx
                //ff c0                   inc    eax
                //c3                      ret

                List<byte> newIL = new List<byte> {0x01, 0xd1};

                int count = 0;

                //Simula um bytecode de no mínimo 500 bytes
                while (newIL.Count < 500)
                {
                    newIL.Add(0x83);
                    newIL.Add(0xc1);
                    newIL.Add((byte) ++count);
                }

                newIL.Add(0x89);
                newIL.Add(0xc8);
                newIL.Add(0xff);
                newIL.Add(0xc0);
                newIL.Add(0xc3);

                return new ReplaceInfo(ReplaceInfo.ReplaceMode.ASM, newIL.ToArray());
            }

            return null;
        }

        public static int Somar(int num1, int num2)
        {
            return num1 + num2;
        }
    }
}