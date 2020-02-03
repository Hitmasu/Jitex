using System.Collections.Generic;
using Jitex.Builder;
using Jitex.PE.Signature;

namespace Testing
{
    class Program
    {
        private static void Main()
        {
            IList<LocalVariableInfo> localVariables = new List<LocalVariableInfo>();
            localVariables.Add(new LocalVariableInfo(typeof(float)));
            localVariables.Add(new LocalVariableInfo(typeof(float)));
            localVariables.Add(new LocalVariableInfo(typeof(float)));
            localVariables.Add(new LocalVariableInfo(typeof(float)));
            localVariables.Add(new LocalVariableInfo(typeof(float)));
            localVariables.Add(new LocalVariableInfo(typeof(float)));
            
            MethodBodyBuilder builder = new MethodBodyBuilder(null, localVariables, null);
            byte[] signature = builder.GetSignature();
            int a = 10;
        }

        public static float ReSomar(int num1, int num2)
        {
            float b = 10;
            float c = 10;
            float a = 10;
            float d = 10;
            float e = 10;
            return a + b + c + d + e;
        }
    }
}