using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Jitex.JIT;
using LocalVariableInfo = Jitex.Builder.LocalVariableInfo;
using MethodBody = Jitex.Builder.MethodBody;

namespace Testing
{
    class Program
    {
        private static void Main()
        {
            ManagedJit managedJit = ManagedJit.GetInstance();
            managedJit.OnPreCompile = OnPreCompile;
            var result = ReSomar();
            Console.WriteLine(result);
            Console.ReadKey();
        }

        private static ReplaceInfo OnPreCompile(MethodBase method)
        {
            MethodInfo Somar = typeof(Program).GetMethod("ReSomar");

            if (Somar.MetadataToken == method.MetadataToken)
            {
                MethodInfo ReplaceSomar = typeof(Program).GetMethod("ReplaceSomar");
                byte[] il = ReplaceSomar.GetMethodBody().GetILAsByteArray();
                List<LocalVariableInfo> variables = ReplaceSomar.GetMethodBody().LocalVariables.Select(lv => new LocalVariableInfo(lv.LocalType)).ToList();
                return new ReplaceInfo(new MethodBody(il, variables, typeof(Program).Module));
            }

            return null;
        }

        public static float ReSomar()
        {
            float c = 10;
            float a = 10;
            float d = 10;
            float e = 10;
            float z = 10;
            return a + c + d + e + z;
        }

        public static float ReplaceSomar()
        {
            float b = 10;
            float c = 10;
            float a = 10;
            float d = 10;
            float e = 10;
            float z = 10;
            int idade = 4500;
            int lol = 900;
            double abc = 500d;
            return (float) (a + b + c + d + e+z + idade + lol+abc);
        }
    }
}