using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Jitex.Intercept
{
    internal class InterceptBuilder
    {
        public MethodBase Method { get; }
        private AssemblyBuilder AssemblyBuilder { get; }
        private ModuleBuilder ModuleBuilder { get; }
        private TypeBuilder TypeBuilder { get; }
        public TypeInfo Type { get; private set; }

        private static readonly MethodInfo InterceptCall;
        private static readonly MethodInfo InterceptGetInstance;
        private static readonly ConstructorInfo ObjectCtor;

        static InterceptBuilder()
        {
            InterceptGetInstance = typeof(InterceptManager).GetMethod(nameof(InterceptManager.GetInstance));
            InterceptCall = typeof(InterceptManager).GetMethod(nameof(InterceptManager.InterceptCall), BindingFlags.Public | BindingFlags.Instance);
            ObjectCtor = typeof(object).GetConstructor(System.Type.EmptyTypes);
        }


        public InterceptBuilder(MethodBase method)
        {
            Method = method;

            AssemblyName assemblyName = new AssemblyName { Name = Method.Module.Assembly.FullName + "JitexDynamicAssembly" };

            AssemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
                assemblyName,
                AssemblyBuilderAccess.Run);

            ModuleBuilder = AssemblyBuilder.DefineDynamicModule(Method.Module.Name + "JitexDynamicModule");
            TypeBuilder = ModuleBuilder.DefineType(Method.DeclaringType.Name + "JitexDynamicType",
                TypeAttributes.Public);
        }

        public MethodBase Create()
        {
            if (Method.IsConstructor)
                return CreateConstructorInterceptor();

            return CreateMethodInterceptor();
        }

        private ConstructorInfo CreateConstructorInterceptor()
        {
            MethodAttributes attributes = MethodAttributes.Public;

            if (Method.IsStatic)
                attributes |= MethodAttributes.Static;

            ParameterInfo[] parameters = Method.GetParameters().ToArray();

            ConstructorBuilder constructorBuilder = TypeBuilder.DefineConstructor(attributes, CallingConventions.HasThis, parameters.Select(w => w.ParameterType).ToArray());
            ILGenerator generator = constructorBuilder.GetILGenerator();

            BuildBody(generator, parameters, typeof(void));

            Type = TypeBuilder.CreateTypeInfo();
            var ci = Type.GetConstructor(parameters.Select(w => w.ParameterType).ToArray());
            Builder.Method.MethodBody body = new Builder.Method.MethodBody(ci);
            var p = body.ReadIL();
            return Type.GetConstructor(parameters.Select(w => w.ParameterType).ToArray());
        }

        private MethodInfo CreateMethodInterceptor()
        {
            MethodAttributes attributes = MethodAttributes.Public;

            if (Method.IsStatic)
                attributes |= MethodAttributes.Static;

            ParameterInfo[] parameters = Method.GetParameters().ToArray();

            MethodInfo methodInfo = (MethodInfo)Method;

            MethodBuilder methodBuilder = TypeBuilder.DefineMethod(Method.Name + "Jitex", attributes, methodInfo.ReturnType, parameters.Select(w => w.ParameterType).ToArray());
            ILGenerator generator = methodBuilder.GetILGenerator();

            BuildBody(generator, parameters, methodInfo.ReturnType);

            Type = TypeBuilder.CreateTypeInfo();
            return Type.GetMethod(methodBuilder.Name);
        }

        private void BuildBody(ILGenerator generator, ParameterInfo[] parameters, Type returnType)
        {
            int totalArgs = parameters.Length;

            if (!Method.IsStatic)
                totalArgs++;

            generator.DeclareLocal(typeof(long));

            if (Method.IsConstructor && !Method.IsStatic)
            {
                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Call, ObjectCtor);
            }

            generator.Emit(OpCodes.Call, InterceptGetInstance);
            generator.Emit(OpCodes.Ldc_I8, Method.MethodHandle.Value.ToInt64());
            generator.Emit(OpCodes.Stloc_0);
            generator.Emit(OpCodes.Ldloc_0);
            generator.Emit(OpCodes.Ldc_I4, totalArgs);
            generator.Emit(OpCodes.Newarr, typeof(object));

            if (totalArgs > 0)
            {
                int argIndex = 0;

                generator.Emit(OpCodes.Dup);

                if (!Method.IsStatic)
                {
                    generator.Emit(OpCodes.Ldc_I4_0);
                    generator.Emit(OpCodes.Ldarg_0);
                    generator.Emit(OpCodes.Stelem_Ref);

                    if (totalArgs > 1)
                    {
                        generator.Emit(OpCodes.Dup);
                        argIndex++;
                    }
                }

                foreach (ParameterInfo parameter in parameters)
                {
                    Type parameterType = parameter.ParameterType;

                    generator.Emit(OpCodes.Ldc_I4, argIndex);
                    generator.Emit(OpCodes.Ldarg_S, argIndex++);

                    if (!(parameterType == typeof(object)))
                        generator.Emit(OpCodes.Box, parameterType);

                    generator.Emit(OpCodes.Stelem_Ref);

                    if (argIndex < totalArgs)
                        generator.Emit(OpCodes.Dup);
                }
            }
            
            generator.Emit(OpCodes.Call, InterceptCall);

            if (returnType == typeof(void))
                generator.Emit(OpCodes.Pop);
            else if (returnType.IsValueType)
                generator.Emit(OpCodes.Unbox_Any, returnType);
            else
                generator.Emit(OpCodes.Castclass, returnType);

            generator.Emit(OpCodes.Ret);
        }
    }
}
