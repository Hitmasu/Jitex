using System;
using System.Diagnostics;
using System.Drawing;
using Jitex;
using Jitex.Intercept;
using Jitex.JIT.Context;
using Jitex.Utils;

namespace ConsoleApp1
{

    public class InterceptPerson
    {
        public InterceptPerson(string name, int age)
        {
            Name = name;
            Age = age;
        }

        public InterceptPerson(string name)
        {
            Name = name;
        }

        public string Name { get; set; }

        public int Age { get; set; }
    }

    class Program
    {
        private static int value = 100;
        private InterceptPerson result;

        static void Main()
        {
            object obj = new object();

            unsafe
            {
                TypedReference tr = __makeref(obj);
                IntPtr address = *(IntPtr*)(&tr);
                Console.WriteLine($"Original variable address {address.ToString("X")}");
            }

            ShowMyAddress(obj);
            Console.ReadKey();
            //JitexManager.AddMethodResolver(MethodResolver);
            //int result = fib(10);
            //Console.WriteLine(result);
        }


        public static void ShowMyAddress(in object obj)
        {
            //unsafe
            //{
            //    TypedReference tr = __makeref(obj);
            //    IntPtr address = *(IntPtr*) (&tr);
            //    Console.WriteLine($"Original variable address {address.ToString("X")}");
            //}

            Debugger.Break();
        }

        public static int fib(int n)
        {
            return -9999;
        }


        private static void MethodResolver(MethodContext context)
        {
            if (context.Method.Name == nameof(fib))
            {
                byte[] asm =
                {
                    0x55,
                    0x48, 0x89, 0xe5,
                    0x53,
                    0x48, 0x83, 0xec, 0x18,
                    0x89, 0x7d, 0xec,
                    0x83, 0x7d, 0xec, 0x02,
                    0x7f, 0x07,
                    0xb8, 0x01, 0x00, 0x00, 0x00,
                    0xeb, 0x1e,
                    0x8b, 0x45, 0xec,
                    0x83, 0xe8, 0x01,
                    0x89, 0xc7,
                    0xe8, 0xda, 0xff, 0xff, 0xff,
                    0x89, 0xc3,
                    0x8b, 0x45, 0xec,
                    0x83, 0xe8, 0x02,
                    0x89, 0xc7,
                    0xe8, 0xcb, 0xff, 0xff, 0xff,
                    0x01, 0xd8,
                    0x48, 0x8b, 0x5d, 0xf8,
                    0xc9,
                    0xc3,
                    0x55,
                    0x48, 0x89, 0xe5,
                    0xb8, 0x00, 0x00, 0x00,
                    0x5d,
                    0xc3,
                    0x66, 0x0f, 0x1f, 0x44, 0x00, 0x00
                };
                context.ResolveNative(asm);
                //context.InterceptCall();
            }
        }
    }
}