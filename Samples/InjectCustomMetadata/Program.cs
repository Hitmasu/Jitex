using System;
using InjectCustomMetadata.Library;

namespace InjectCustomMetadata
{
    class Program
    {
        static void Main(string[] args)
        {
            ExternLibrary.Initialize();
            int result = SimpleSum(1, 7);
            Console.WriteLine(result);
        }

        static int SimpleSum(int num1, int num2)
        {
            return num1 + num2;
        }
    }
}