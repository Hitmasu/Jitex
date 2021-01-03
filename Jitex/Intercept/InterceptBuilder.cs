using System;
using System.Diagnostics;
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
        public Type Type { get; private set; }

        private static readonly MethodInfo InterceptCall;
        private static readonly MethodInfo InterceptGetInstance;
        private static readonly MethodInfo GetTypeFromHandle;
        private static readonly ConstructorInfo ObjectCtor;

        static InterceptBuilder()
        {
            InterceptGetInstance = typeof(InterceptManager).GetMethod(nameof(InterceptManager.GetInstance));
            InterceptCall = typeof(InterceptManager).GetMethod(nameof(InterceptManager.InterceptCall), BindingFlags.Public | BindingFlags.Instance);
            GetTypeFromHandle = typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle));
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
            TypeBuilder = BuildType();
        }

        public MethodBase Create()
        {
            if (Method.IsConstructor)
                return CreateConstructorInterceptor();

            return CreateMethodInterceptor();
        }

        private Type CreateType()
        {
            Type type = TypeBuilder.CreateTypeInfo();

            if (Method.DeclaringType.IsGenericType)
                type = type.MakeGenericType(Method.DeclaringType.GetGenericArguments());

            return type;
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

            Type = CreateType();
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

            if (Method.IsGenericMethod)
            {
                Type[] genericArguments = ((MethodInfo)Method).GetGenericMethodDefinition().GetGenericArguments();
                methodBuilder.DefineGenericParameters(genericArguments.Select(w => w.Name).ToArray());
            }

            ILGenerator generator = methodBuilder.GetILGenerator();

            BuildBody(generator, parameters, methodInfo.ReturnType);

            Type = CreateType();

            MethodInfo method = Type.GetMethod(methodBuilder.Name);

            if (method.IsGenericMethod)
            {
                Type[] genericArguments = ((MethodInfo)Method).GetGenericArguments();
                method = method.MakeGenericMethod(genericArguments);
            }

            return method;
        }

        private TypeBuilder BuildType()
        {
            TypeBuilder type = ModuleBuilder.DefineType(Method.DeclaringType.Name + "JitexDynamicType",
                TypeAttributes.Public);

            if (!Method.DeclaringType.IsGenericType)
                return type;

            Type[] genericArguments = Method.DeclaringType.GetGenericTypeDefinition().GetGenericArguments();
            type.DefineGenericParameters(genericArguments.Select(w => w.Name).ToArray());

            return type;
        }

        private void BuildBody(ILGenerator generator, ParameterInfo[] parameters, Type returnType)
        {
            void LoadGenericTypes(Type[] types)
            {
                generator.Emit(OpCodes.Ldc_I4, types.Length);
                generator.Emit(OpCodes.Newarr, typeof(Type));

                generator.Emit(OpCodes.Dup);

                for (int i = 0; i < types.Length; i++)
                {
                    Type genericMethodArgument = types[i];
                    generator.Emit(OpCodes.Ldc_I4, i);
                    generator.Emit(OpCodes.Ldtoken, genericMethodArgument);
                    generator.Emit(OpCodes.Call, GetTypeFromHandle);
                    generator.Emit(OpCodes.Stelem_Ref);

                    if (i < types.Length - 1)
                        generator.Emit(OpCodes.Dup);
                }
            }

            int totalArgs = parameters.Length;

            if (!Method.IsConstructor && !Method.IsStatic)
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

            if (totalArgs > 0)
            {
                generator.Emit(OpCodes.Ldc_I4, totalArgs);
                generator.Emit(OpCodes.Newarr, typeof(object));

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
            else
            {
                generator.Emit(OpCodes.Ldnull);
            }

            if (TypeBuilder.IsGenericType)
            {
                Type[] genericTypeArguments = ((MethodInfo)Method).GetGenericMethodDefinition().GetGenericArguments();
                LoadGenericTypes(genericTypeArguments);
            }
            else
            {
                generator.Emit(OpCodes.Ldnull);
            }

            if (Method.IsGenericMethod)
            {
                Type[] genericMethodArguments = ((MethodInfo)Method).GetGenericMethodDefinition().GetGenericArguments();
                LoadGenericTypes(genericMethodArguments);
            }
            else
            {
                generator.Emit(OpCodes.Ldnull);
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