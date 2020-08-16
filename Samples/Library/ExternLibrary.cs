using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using Jitex.JIT;
using Jitex.JIT.CorInfo;
using MethodBody = Jitex.Builder.MethodBody;

namespace Library
{
    /// <summary>
    /// Example of a external library to replace SimpleSum.
    /// </summary>
    /// <remarks>
    /// We replace SimpleSum to return the PID of process. To do this, normally we need
    /// reference assembly (System.Diagnostics.Process) and class Process.
    /// In this case, the original module, dont have any reference to Diagnostics and class Process.
    /// As we pass a MetadataToken from Process.GetCurrentProcess().Id, its necessary resolve that manually,
    /// because CLR dont have any information about that in original module.
    /// </remarks>
    public static class ExternLibrary
    {

        private static MethodInfo _getCurrentProcess;
        private static MethodInfo _getterId;

        static ExternLibrary()
        {
            LoadAssemblyDiagnostics();
        }

        public static void Initialize()
        {
            ManagedJit jitex = ManagedJit.GetInstance();

            jitex.AddCompileResolver(CompileResolve);
            jitex.AddTokenResolver(TokenResolve);
        }

        private static void LoadAssemblyDiagnostics()
        {
            string pathAssembly = Path.Combine(Directory.GetCurrentDirectory(),"../../../../", "System.Diagnostics.Process.dll");

            Assembly assemblyDiagnostics = Assembly.LoadFrom(pathAssembly);
            Type typeProcess = assemblyDiagnostics.GetType("System.Diagnostics.Process");
            _getCurrentProcess = typeProcess.GetMethod("GetCurrentProcess");
            _getterId = _getCurrentProcess.ReturnType.GetProperty("Id", BindingFlags.Public | BindingFlags.Instance).GetGetMethod();
        }

        private static void TokenResolve(TokenContext context)
        {
            if (context.TokenType == TokenKind.Method && context.Source.Name == "SimpleSum")
            {
                if (context.MetadataToken == _getCurrentProcess.MetadataToken)
                {
                    context.ResolveMethod(_getCurrentProcess);
                }
                else if (context.MetadataToken == _getterId.MetadataToken)
                {
                    context.ResolveMethod(_getterId);
                }
            }
        }

        private static void CompileResolve(CompileContext context)
        {
            if (context.Method.Name == "SimpleSum")
            {
                List<byte> newIl = new List<byte>();

                newIl.Add((byte)OpCodes.Call.Value);
                newIl.AddRange(BitConverter.GetBytes(_getCurrentProcess.MetadataToken));
                newIl.Add((byte)OpCodes.Call.Value);
                newIl.AddRange(BitConverter.GetBytes(_getterId.MetadataToken));
                newIl.Add((byte)OpCodes.Ret.Value);

                MethodBody methodBody = new MethodBody(newIl.ToArray(), _getCurrentProcess.Module);

                context.ResolveBody(methodBody);
            }
        }
    }
}
