using Jitex.JIT;
using Jitex.PE;
using System.Reflection;

namespace Testing
{
    public class T
    {
        private int a = 10;
    }

    class Program
    {
        private static void Main()
        {
            MetadataInfo info = new MetadataInfo(typeof(Program).Assembly);
        }

        private static ReplaceInfo OnPreCompile(MethodBase method)
        { 
            MethodInfo somarInfo = typeof(Program).GetMethod("Somar");
            
            if (somarInfo.MetadataToken == method.MetadataToken)
            {
                MethodInfo methodHelper = typeof(Program).GetMethod("ReSomar");
                //num1 + num2 + 1 + 2 + 3 + 4 + 5 + 6 + ....

                //01 d1                   add    ecx,edx
                //83 c1 0a                add    ecx,0x1
                //83 c1 14                add    ecx,0x2
                //83 c1 1e                add    ecx,0x3
                //83 c1 28                add    ecx,0wx4
                //...
                //89 c8                   mov    eax,ecx
                //ff c0                   inc    eax
                //c3                      ret

                // List<byte> newIL = new List<byte> {0x01, 0xd1};
                //
                // int count = 0;
                //
                // //Simula um bytecode de no mínimo 500 bytes
                // while (newIL.Count < 30)
                // {
                //     newIL.Add(0x83);
                //     newIL.Add(0xc1);
                //     newIL.Add((byte) ++count);
                // }
                //
                // newIL.Add(0x89);
                // newIL.Add(0xc8);
                // newIL.Add(0xff);
                // newIL.Add(0xc0);
                // newIL.Add(0xc3);
                //
                return new ReplaceInfo(ReplaceInfo.ReplaceMode.IL, methodHelper.GetMethodBody().GetILAsByteArray());
                
                
            }

            return null;
        }

        public static int Somar(int num1, int num2)
        {
            float x = 10;
            float w = 10;
            float q = 10;
            double a = 10;
            double q1 = 10;
            double q2 = 10;
            int b = 20;
            // int c = 30;
            int d = 40;
            int e = 50;
            ManagedJit c = null;
            c.OnPreCompile = OnPreCompile;
            return (int) (a + b + 0 + d + e + w + x +q);
        }       
        
        public static int ReSomar(int num1, int num2)
        {
            int a = 10;
            int b = 20;
            int c = 30;
            return a + b + c;
        }
    }
}