using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Jitex.Internal
{
    public class StubBuilder
    {
        private static readonly MethodInfo GetMethodToken;

        static StubBuilder()
        {
            GetMethodToken = typeof(ILGenerator).GetMethod("GetMethodToken", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        public (Module module, int token) GenerateStubToken(MethodInfo originalMethod)
        {
            AssemblyBuilder assemblyDynamic = AssemblyBuilder.DefineDynamicAssembly(
                new AssemblyName { Name = "JitexDynamicAssembly" },
                AssemblyBuilderAccess.Run);

            ModuleBuilder moduleDynamic = assemblyDynamic.DefineDynamicModule("JitexDynamicModule");

            TypeBuilder typeDynamic = moduleDynamic.DefineType("JitexDynamicType",
                TypeAttributes.Public);

            MethodAttributes flags = MethodAttributes.Public;

            if (originalMethod.IsStatic)
                flags |= MethodAttributes.Static;

            MethodBuilder myMthdBld = typeDynamic.DefineMethod(originalMethod.Name, flags, CallingConventions.Standard, originalMethod.DeclaringType, originalMethod.GetParameters().Select(w => w.ParameterType).ToArray());

            ILGenerator ilGenerator = myMthdBld.GetILGenerator();
            ilGenerator.Emit(OpCodes.Ret);

            MethodInfo method = typeDynamic.CreateTypeInfo().GetMethod(originalMethod.Name);

            int token = GenerateToken(ilGenerator,originalMethod);

            return (method.Module, token);
        }
        
        static int GenerateToken(ILGenerator ilGenerator, MethodBase method)
        {
            Type[] optionalParameters = method.GetParameters().Where(w => w.IsOptional).Select(w => w.ParameterType).ToArray();
            int token = (int)GetMethodToken.Invoke(ilGenerator, new object[] { method, optionalParameters, false });
            return token;
        }
    }
}
