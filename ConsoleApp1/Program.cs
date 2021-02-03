using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using Jitex;
using Jitex.JIT.Context;
using Jitex.Utils;

namespace ConsoleApp1
{

    public class InterceptPerson
    {
        private string _name;
        public int Age { get; set; }

        public InterceptPerson()
        {

        }
        public InterceptPerson(string name)
        {
            _name = name;
        }

        public ref string GetName()
        {
            if (_name != "Teste")
            {
                object abc = this;
                IntPtr addr = TypeHelper.GetReferenceFromObject(ref abc);
                Console.WriteLine($"Addr: {addr.ToString("X")}");
                Console.WriteLine($"NAME DIFFERENT!: {_name}");
            }

            return ref _name;
        }
    }

    class Program
    {
        static void Main()
        {
            JitexManager.AddMethodResolver(MethodResolver);

            InterceptPerson person = new InterceptPerson("Teste");
            //Console.WriteLine(typeof(InterceptPerson).TypeHandle.Value.ToString("X"));

            IntPtr addr = TypeHelper.GetReferenceFromObject(ref person);
            Console.WriteLine("0x" + addr.ToString("X"));
            int count = 0;
            do
            {
                string result = person.GetName();

                if (result != "Teste")
                {
                    Console.WriteLine(count);
                    Debugger.Break();
                }

                count++;
            } while (true);
        }

        private static void MethodResolver(MethodContext context)
        {
            if (context.Method.Name == "GetName")
            {
                context.InterceptCall();
            }
        }

        public sealed class ObjectPin : IDisposable
        {
            public AutoResetEvent Reset { get; set; }
            public object Object{get; private set;}


            public ObjectPin(object obj)
            {
                Reset = new AutoResetEvent(false);
                //Object = obj;

                using AutoResetEvent lockMethod = new AutoResetEvent(false);

                Thread thread = new Thread(() =>
                {
                    HoldMethod(obj, lockObj =>
                    {
                        lockMethod.Set();
                        ref object lObj = ref lockObj;
                        Reset.WaitOne();
                        Console.WriteLine(lObj.ToString());
                    });
                });

                thread.Start();
                lockMethod.WaitOne();
            }

            public void Dispose()
            {
                Reset.Set();
                Reset?.Dispose();
            }

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
            private unsafe static void HoldMethod(object obj, Action<object> method)
            {
                object holdObject = obj;
                method(holdObject);
            }
        }
    }
}