using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Iced.Intel;
using Jitex;
using Jitex.Intercept;
using Jitex.JIT.Context;
using Jitex.Utils;
using static Iced.Intel.AssemblerRegisters;

namespace ConsoleApp1
{

    public record InterceptPerson
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


        public string GetAgeAfter10Years(string n1, string n2)
        {
            IntPtr addr = TypeUtils.GetAddressFromObject(ref n1);
            IntPtr addr2 = TypeUtils.GetAddressFromObject(ref n2);

            Console.WriteLine(addr.ToString("X"));
            Console.WriteLine(addr2.ToString("X"));

            return $"{n1}{n2}";
        }
    }

    class Program
    {
        static void Main()
        {
            JitexManager.AddMethodResolver(MethodResolver);
            JitexManager.AddInterceptor(Interceptor);

            string n1 = "Flávio";
            IntPtr address = TypeUtils.GetAddressFromObject(ref n1);
            Console.WriteLine(address.ToString("X"));
            GetAgeAfter10Years(ref n1);
            Debugger.Break();
        }

        private static string ab = "la";

        public static int GetRef()
        {
            return 0;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string GetAgeAfter10Years(ref string a)
        {
            IntPtr address = TypeUtils.GetAddressFromObject(ref a);
            Console.WriteLine(address.ToString("X"));


            return "aspodkaspodk";
        }

        private static void Interceptor(CallContext context)
        {
            string a = "xyz";
            object obj = a;

            context.Parameters.OverrideParameterValue(0, obj);

            context.Continue<string>();
        }

        private static void MethodResolver(MethodContext context)
        {
            if (context.Method.Name == "GetAgeAfter10Years")
                context.InterceptCall();
        }
    }

    internal class Gen<T>
    {
        public static void Get()
        {
            Console.WriteLine(nameof(T));
        }
    }
}