using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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

        ~InterceptPerson()
        {
            Debugger.Break();
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
        unsafe static void Main()
        {
            JitexManager.AddMethodResolver(MethodResolver);
            JitexManager.AddInterceptor(Interceptor);

            Program p = new Program();
            int a = 20;

            var l = __makeref(a);
            IntPtr addr = *(IntPtr*)&l;

            Console.WriteLine($"Reference: 0x{addr.ToString("X")}");
            Console.WriteLine($"Value: 0x{Marshal.ReadIntPtr(addr).ToString("X")}");


            InterceptPerson person = new InterceptPerson("aqsd", 10);
            int age = p.SumAge(person);
            Console.WriteLine(age);
            Console.ReadKey();
        }

        private int SumAge(InterceptPerson b)
        {
            return b.Age + 30;
        }

        private static void Interceptor(CallContext context)
        {
            if (context.Method.Name == nameof(SumAge))
            {
                InterceptPerson person = context.Parameters.GetParameterValue<InterceptPerson>(0);
                person.Age += 50;

                context.Parameters.SetParameterValue(0, person);
            }
        }

        private static void MethodResolver(MethodContext context)
        {
            if (context.Method.Name == nameof(SumAge))
            {
                context.InterceptCall();
            }
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