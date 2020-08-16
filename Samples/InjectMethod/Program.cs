using System;
using System.Net.Http;
using System.Reflection;
using Jitex.JIT;

namespace InjectMethod
{
    class Program
    {
        static void Main(string[] args)
        {
            ManagedJit jit = ManagedJit.GetInstance();
            jit.AddCompileResolver(CompileResolver);
            int result = SimpleSum(5, 5);
            Console.WriteLine(result);
            Console.ReadKey();
        }

        ///<summary>
        ///Simple method to override.
        ///</summary>
        static int SimpleSum(int num1, int num2)
        {
            return num1 + num2;
        }

        /// <summary>
        /// Take sum of 2 random numbers
        /// </summary>
        /// <returns></returns>
        public static int SimpleSumReplace()
        {
            const string url = "https://www.random.org/integers/?num=2&min=1&max=999&col=2&base=10&format=plain&rnd=new";
            using HttpClient client = new HttpClient();
            using HttpResponseMessage response = client.GetAsync(url).Result;
            string content = response.Content.ReadAsStringAsync().Result;
            string[] columns = content.Split("\t");

            int num1 = int.Parse(columns[0]);
            int num2 = int.Parse(columns[1]);

            return num1 + num2;
        }

        private static void CompileResolver(CompileContext context)
        {
            if (context.Method.Name == "SimpleSum")
            {
                //Replace SimpleSum to our SimpleSumReplace
                MethodInfo replaceSumMethod = typeof(Program).GetMethod("SimpleSumReplace");
                context.ResolveMethod(replaceSumMethod);
            }
        }
    }
}
