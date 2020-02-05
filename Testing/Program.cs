using Jitex.Builder;
using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Testing
{
    class Program
    {
        [MethodImpl (MethodImplOptions.InternalCall)]
        internal static extern CorElementType GetCorElementType (RuntimeTypeHandle type);
        
        private static void Main()
        {
            MethodInfo t = typeof(Program).GetMethod("ReSomar");
            MethodBodyBuilder builder = new MethodBodyBuilder(t.GetMethodBody().GetILAsByteArray(), typeof(Program).Module);
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