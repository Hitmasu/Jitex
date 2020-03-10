using FrameworkTest;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Jitex.Builder;
using Jitex.JIT;

namespace ProgramTest
{
    class Program
    {
        static void Main(string[] args)
        {
            //Framework framework = new Framework(typeof(Program).Module);
            ManagedJit jit = ManagedJit.GetInstance();
            jit.OnPreCompile = OnPreCompile;





            //int resultado = (int)dm.Invoke(null, null);

            //var c = new  MethodBody(dm);

            //Trace.WriteLine("SomaTemp to be called");
            ////Debugger.Break();
            //Trace.WriteLine("Pós debugger");
            //int resultado = SomarTemp();
            //Trace.WriteLine(resultado);
            //Trace.WriteLine("SomarToReplace to be called");
            ////Debugger.Break();
            //Trace.WriteLine("Pós debugger");
            SomarReplace.SomarToReplace();
            int resultado = Somar();
            Console.WriteLine(resultado);
            Console.ReadKey();
            //Trace.WriteLine(resultado);
        }

        private static ReplaceInfo OnPreCompile(MethodBase method)
        {
            if (method.Name == "Somar")
            {
                var methodToInject = typeof(SomarReplace).GetMethod("SomarToReplace");
                //var methodToInject = typeof(Program).GetMethod("SomarTemp");
                //RuntimeHelpers.PrepareMethod(methodToInject.MethodHandle);
                //IntPtr pointer = methodToInject.MethodHandle.GetFunctionPointer();
                //if (Marshal.ReadByte(pointer) == 0xE9)
                //{
                //    int offset = Marshal.ReadInt32(pointer + 1);
                //    pointer += offset + 5;
                //}

                //DynamicMethod dm = new DynamicMethod("teste", typeof(int),null,method.Module);
                //var generator = dm.GetILGenerator();

                //generator.Emit(OpCodes.Ldftn, methodToInject);
                //generator.EmitCalli(OpCodes.Calli, CallingConvention.StdCall, methodToInject.ReturnType, null);
                //generator.Emit(OpCodes.Ret);
                //int result = (int)dm.Invoke(null, null);
                return new ReplaceInfo(new Jitex.Builder.MethodBody(methodToInject));
            }

            return null;
        }

        public static int SomarTemp()
        {
            return 99 + 99;
        }

        public static int Somar()
        {
            return 10 + 10;
        }
    }
}
