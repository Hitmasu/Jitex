using Jitex.JIT;
using Jitex.PE;
using System;
using FrameworkTeste;

namespace Testing
{
    class Program
    {
        private static MetadataInfo _metadata;
        private static void Main()
        {
            Initializer initialzer = new Initializer(typeof(Program).Module);
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