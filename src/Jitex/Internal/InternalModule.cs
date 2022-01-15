using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Jitex.JIT.Context;
using Jitex.Utils;

namespace Jitex.Internal
{
    internal class InternalModule : JitexModule
    {
        private static readonly IDictionary<IntPtr, Module> MethodSpecCache = new Dictionary<IntPtr, Module>();

        public static InternalModule Instance { get; } = new InternalModule();

        /// <summary>
        /// Tokens to be resolved
        /// </summary>
        /// <remarks>
        /// Key: (IntPtr module, int metadataToken)
        /// Value: Member resolution.
        /// </remarks>
        private readonly ConcurrentDictionary<Tuple<IntPtr, int>, MemberInfo> _methodResolutions = new();

        protected override void MethodResolver(MethodContext context)
        {
        }

        protected override void TokenResolver(TokenContext context)
        {
            if (context.Module == null)
                return;

            if (!_methodResolutions.TryGetValue(new(context.Scope, context.MetadataToken), out MemberInfo resolution))
                return;

            if (resolution is MethodInfo {IsGenericMethod: true} method)
            {
                Module module = GetMethodSpecFor(method);
                context.ResolverMember(module, 0x2b000001);
            }
            else
            {
                context.ResolverMember(resolution);
            }
        }

        /// <summary>
        /// Add a token to be resolved.
        /// </summary>
        /// <param name="module">Module from token.</param>
        /// <param name="metadataToken">Token to be resolved.</param>
        /// <param name="memberResolution">Member identifier from token.</param>
        public void AddMemberToResolution(Module module, int metadataToken, MemberInfo memberResolution)
        {
            IntPtr handle = AppModules.GetHandleFromModule(module);
            _methodResolutions.TryAdd(new(handle, metadataToken), memberResolution);
        }

        private static Module GetMethodSpecFor(MethodInfo method)
        {
            IntPtr handle = MethodHelper.GetMethodHandle(method).Value;

            if (!MethodSpecCache.TryGetValue(handle, out Module module))
            {
                module = CreateMethodSpecFor(method);
                MethodSpecCache.Add(handle, module);
            }

            return module;
        }

        private static Module CreateMethodSpecFor(MethodInfo method)
        {
            AssemblyName asmName = new("MethodSpecGeneratorAssembly");
            AssemblyBuilder methodSpecAssembly = AssemblyBuilder.DefineDynamicAssembly(asmName, AssemblyBuilderAccess.Run);

            ModuleBuilder methodSpecModule = methodSpecAssembly.DefineDynamicModule("MethodSpecGeneratorModule");
            TypeBuilder methodSpecType = methodSpecModule.DefineType("MethodSpecGeneratorType", TypeAttributes.Public);

            MethodBuilder methodSpecBuilder = methodSpecType.DefineMethod("MethodGeneratorMethod", MethodAttributes.Public | MethodAttributes.Static);

            methodSpecBuilder.SetReturnType(typeof(void));

            ILGenerator generator = methodSpecBuilder.GetILGenerator();
            generator.Emit(OpCodes.Call, method);
            generator.Emit(OpCodes.Ret);

            return methodSpecType.CreateTypeInfo()!.Module;
        }
    }
}