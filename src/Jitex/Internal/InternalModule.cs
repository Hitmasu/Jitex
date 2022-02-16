using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Reflection.Emit;
using Jitex.JIT.Context;
using Jitex.JIT.CorInfo;
using Jitex.PE;
using Jitex.Utils;
using MethodInfo = System.Reflection.MethodInfo;
using Pointer = Jitex.Utils.Pointer;

namespace Jitex.Internal
{
    public class InternalModule : JitexModule
    {
        private static readonly MethodInfo GetTypeFromHandle = typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle))!;


        public static InternalModule Instance { get; } = new();

        /// <summary>
        /// Tokens to be resolved
        /// </summary>
        private readonly ConcurrentDictionary<(Module Module, int MetadataToken), MemberInfo> _methodResolutions = new();

        /// <summary>
        /// Method specs to be resolved
        /// </summary>
        private static readonly Dictionary<(Module Module, int MetadataToken), Module> MethodSpecCache = new();

        protected override void MethodResolver(MethodContext context)
        {
        }

        protected override void TokenResolver(TokenContext context)
        {
            if (context.Module == null)
                return;

            if (!_methodResolutions.TryGetValue((context.Module, context.MetadataToken), out MemberInfo resolution))
                return;

            if (context.TokenType is TokenKind.LdToken or TokenKind.Constrained)
                context.ResolverMember(resolution.Module, context.MetadataToken);
            else
                context.ResolverMember(resolution);
        }

        /// <summary>
        /// Add a token to be resolved.
        /// </summary>
        /// <param name="module">Module from token.</param>
        /// <param name="metadataToken">Token to be resolved.</param>
        /// <param name="memberResolution">Member identifier from token.</param>
        public void AddMemberToResolution(Module module, int metadataToken, MemberInfo memberResolution)
        {
            _methodResolutions.TryAdd(new(module, metadataToken), memberResolution);

            if (memberResolution is MethodInfo methodInfo)
                LoadMethodSpec(methodInfo);
        }

        /// <summary>
        /// Load specification (TypeSpec and MethodSpec) from a MethodInfo.
        /// </summary>
        /// <param name="methodInfo"></param>
        public void LoadMethodSpec(MethodInfo methodInfo)
        {
            if (!methodInfo.IsGenericMethod)
                return;

            (Module Module, int MetadataToken) key = (methodInfo.Module, methodInfo.MetadataToken);

            if (!MethodSpecCache.ContainsKey(key))
            {
                Module specModule = CreateMethodSpecFor(methodInfo).Module;
                MethodSpecCache[key] = specModule;
            }
        }

        private MethodBase CreateMethodSpecFor(MethodInfo method)
        {
            const string methodName = "MethodSpecGenerated";

            AssemblyName asmName = new("MethodSpecGeneratorAssembly");
            AssemblyBuilder methodSpecAssembly = AssemblyBuilder.DefineDynamicAssembly(asmName, AssemblyBuilderAccess.Run);

            ModuleBuilder methodSpecModule = methodSpecAssembly.DefineDynamicModule("MethodSpecGeneratorModule");
            TypeBuilder methodSpecType = methodSpecModule.DefineType("MethodSpecGeneratorType", TypeAttributes.Public);

            MethodBuilder methodBuilder = methodSpecType.DefineMethod(methodName, MethodAttributes.Public | MethodAttributes.Static);

            int totalGenericArguments = method.GetGenericArguments().Length;

            string[] genericParametersName = new string[totalGenericArguments];

            for (int i = 0; i < totalGenericArguments; i++)
                genericParametersName[i] = $"T{i}";

            GenericTypeParameterBuilder[] genericParameters = methodBuilder.DefineGenericParameters(genericParametersName);
            methodBuilder.SetReturnType(method.ReturnType);

            ILGenerator generator = methodBuilder.GetILGenerator();
            generator.Emit(OpCodes.Ldc_I4_S, totalGenericArguments);
            generator.Emit(OpCodes.Newarr, typeof(Type));

            for (int i = 0; i < totalGenericArguments; i++)
            {
                generator.Emit(OpCodes.Dup);
                generator.Emit(OpCodes.Ldc_I4_S, i);
                generator.Emit(OpCodes.Ldtoken, genericParameters[i]);
                generator.Emit(OpCodes.Call, GetTypeFromHandle);
                generator.Emit(OpCodes.Stelem_Ref);
            }

            generator.Emit(OpCodes.Pop);


            if (!method.IsStatic)
            {
                methodBuilder.SetParameters(method.DeclaringType!);
                generator.Emit(OpCodes.Ldarg_0);
            }

            generator.Emit(OpCodes.Call, method);
            generator.Emit(OpCodes.Ret);

            TypeInfo createdType = methodSpecType.CreateTypeInfo()!;
            MethodBase createdMethod = createdType.GetMethod(methodName)!;

            for (int i = 0; i < totalGenericArguments; i++)
            {
                int metadataToken = MetadataTokenBase.TypeSpec + i;
                _methodResolutions.TryAdd((method.Module, metadataToken), createdMethod);
            }

            return createdMethod;
        }
    }
}