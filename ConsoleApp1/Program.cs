using System;
using Jitex;
using Jitex.JIT.Context;

//using Jitex;
//using Jitex.JIT.Context;

namespace ConsoleApp1
{
    class A
    {

    }

    class B
    {

    }

    class Program
    {
        static void A<T>(T a)
        {
            
        }

        static void B<T>() where T : new()
        {
            T a = new T();
            A<T>(a);
        }

        static void Main(string[] args)
        {
            //var m_pData = Type.GetType("System.Reflection.RuntimeModule").GetField("m_pData", BindingFlags.NonPublic | BindingFlags.Instance);
            //var methodToDetour = typeof(Program).GetMethod("Hook").MakeGenericMethod(typeof(int));

            //DynamicMethod dm = new DynamicMethod("teste", typeof(void), Type.EmptyTypes,methodToDetour.Module);
            //var generator = dm.GetILGenerator();
            //generator.DeclareLocal(typeof(bool));
            //generator.EmitCall(OpCodes.Call, methodToDetour, null);
            //generator.Emit(OpCodes.Pop);
            //generator.Emit(OpCodes.Ret);

            //string value = ((IntPtr)m_pData.GetValue(dm.Module)).ToString("X");
            //Console.WriteLine(value);


            //dm.Invoke(null, null);

            //ShowTeste();                          
            //Console.ReadKey();
            JitexManager.AddMethodResolver(MethodResolver);
            ShowTeste();
            Console.ReadKey();
        }

        public static void ShowTeste()
        {
            ShowMe<Program>();
        }

        private static void MethodResolver(MethodContext context)
        {
            if (context.Method.Name == "ShowMe")
            {
                var methodToDetour = typeof(Program).GetMethod("Hook").MakeGenericMethod(typeof(int));
                context.DetourMethod(methodToDetour);
            }
        }

        private static void TokenResolver(TokenContext context)
        {
            if (context.Source?.Name == "ShowMe")
            {
                //context.ResolveContext();
            }
        }

        public static A ShowMe<A>()
        {
            return default;
        }

        public static P Hook<P>()
        {
            Console.WriteLine("Hooked");
            return default;
        }
    }
}