using System;
using InjectCustomString.Library;

namespace InjectCustomString
{
    class Program
    {
        static void Main(string[] args)
        {
            ExternLibrary.Initialize();
            HelloWorld();
            Console.ReadKey();
        }

        static void HelloWorld()
        {
            Console.WriteLine("Hello World!");
        }
    }
}