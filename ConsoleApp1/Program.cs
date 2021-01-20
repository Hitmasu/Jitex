using System;
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
            JitexManager.AddMethodResolver(MethodResolver);
            JitexManager.AddInterceptor(Interceptor);

            int n1 = 1;
            int n2 = 2;
            int n3;
            SumAge(ref n1, ref n2, out n3);
            Console.WriteLine(n3);
            Console.ReadKey();
        }


        private static void SumAge(ref int n1, ref int n2, out int result)
        {
            result = n1 + n2;
        }

        private static void Interceptor(CallContext context)
        {
            //if (context.Method.Name == nameof(SumAge))
            //{
            //    Point p = new Point(0xFF, 0xFF);
            //    context.Parameters.OverrideParameterValue(0, p);
            //}
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