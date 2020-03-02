using Jitex.JIT;
using System;
using System.Collections.Generic;
using System.Linq;
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

        public static double Somar()
        {
            return 10;
        }
    }
}