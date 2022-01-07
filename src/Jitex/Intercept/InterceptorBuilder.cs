using System;
using System.Collections.Generic;
using System.Linq;
using Jitex.PE;
using System.Reflection;
using System.Reflection.Emit;
using Jitex.Builder.IL;
using LocalVariableInfo = Jitex.Builder.Method.LocalVariableInfo;
using MethodBody = Jitex.Builder.Method.MethodBody;
using Pointer = Jitex.Utils.Pointer;

namespace Jitex.Intercept
{
    internal class InterceptorBuilder : IDisposable
    {
        private static readonly ConstructorInfo CallContextCtor;
        private static readonly ConstructorInfo CallManagerCtor;

        private static readonly MethodInfo CallInterceptors;
        private static readonly MethodInfo SetResult;
        private static readonly MethodInfo GetResult;
        private static readonly MethodInfo ReleaseTask;
        private static readonly MethodInfo PointerBox;

        private readonly MethodBase _method;
        private readonly MethodBody _body;

        private ImageReader? _imageReader;
        private ImageInfo? _image;

        static InterceptorBuilder()
        {
            CallContextCtor = typeof(CallContext).GetConstructor(new[] {typeof(object[])})!;
            CallManagerCtor = typeof(CallManager).GetConstructor(new[] {typeof(CallContext)})!;
            CallInterceptors = typeof(CallManager).GetMethod(nameof(CallManager.CallInterceptors), BindingFlags.Public | BindingFlags.Instance)!;
            ReleaseTask = typeof(CallManager).GetMethod(nameof(CallManager.ReleaseTask), BindingFlags.Public | BindingFlags.Instance)!;
            PointerBox = typeof(Pointer).GetMethod(nameof(Pointer.Box))!;

            PropertyInfo result = typeof(CallContext).GetProperty(nameof(CallContext.Result))!;
            GetResult = result.GetGetMethod();
            SetResult = result.GetSetMethod();
        }

        public InterceptorBuilder(MethodBase method, MethodBody body)
        {
            _method = method;
            _body = body;
        }

        public MethodBody InjectInterceptor()
        {
            if (_imageReader == null)
            {
                _imageReader = new(_method.Module);
                _image = _imageReader.LoadImage();
            }

            IList<Type> parameters = new List<Type>(_method.GetParameters().Select(s => s.ParameterType));

            MethodInfo? methodInfo = _method as MethodInfo;

            if (methodInfo != null && !methodInfo.IsStatic)
                parameters.Add(methodInfo.DeclaringType);

            IList<LocalVariableInfo> localVariables = _body.LocalVariables ?? new List<LocalVariableInfo>();
            localVariables.Add(new(typeof(CallManager)));

            int callManagerInstanceIndex = localVariables.Count - 1;

            _image!.AddTypeRef(typeof(object), out int objCtorMetadataToken);
            _image!.AddMemberRef(CallContextCtor, out int callContextCtorMetadataToken);
            _image!.AddMemberRef(CallManagerCtor, out int callManagerCtorMetadataToken);
            _image!.AddMemberRef(CallInterceptors, out int callInterceptorMetadataToken);
            _image!.AddMemberRef(SetResult, out int setResultMetadataToken);
            _image!.AddMemberRef(GetResult, out int getResultMetadataToken);
            _image!.AddMemberRef(ReleaseTask, out int releaseTaskMetadataToken);
            _image!.AddMemberRef(PointerBox, out int pointerBoxMetadataToken);

            Instructions instructions = new();

            instructions.Add(OpCodes.Ldc_I4_S, parameters.Count);
            instructions.Add(OpCodes.Newarr, objCtorMetadataToken);

            for (int i = 0; i < parameters.Count; i++)
            {
                Type parameter = parameters[i];

                instructions.Add(OpCodes.Dup);
                instructions.Add(OpCodes.Ldc_I4_S, i);
                instructions.Add(OpCodes.Ldarg_S, i);

                //TODO: Maybe set variable as pinned too?
                if (parameter.IsPointer)
                {
                    instructions.Add(OpCodes.Call, pointerBoxMetadataToken);
                }

                if (parameter.IsByRef)
                {
                    instructions.Add(OpCodes.Conv_U);
                    instructions.Add(OpCodes.Call, pointerBoxMetadataToken);
                }
                else if (parameter.IsValueType)
                {
                    _image.AddTypeRef(parameter, out int parameterMetadataToken);
                    instructions.Add(OpCodes.Box, parameterMetadataToken);
                }

                instructions.Add(OpCodes.Stelem_Ref);
            }

            instructions.Add(OpCodes.Newobj, callContextCtorMetadataToken);
            instructions.Add(OpCodes.Dup);
            instructions.Add(OpCodes.Newobj, callManagerCtorMetadataToken);
            instructions.Add(OpCodes.Stloc_S, callManagerInstanceIndex);
            instructions.Add(OpCodes.Ldloc_S, callManagerInstanceIndex);
            instructions.Add(OpCodes.Callvirt, callInterceptorMetadataToken);
            instructions.Add(OpCodes.Dup);

            instructions.AddRange(_body.ReadIL());
            instructions.RemoveLast(); //Remove Ret instruction.

            Instruction? retInstruction = null;

            if (methodInfo != null)
            {
                Type returnType = methodInfo.ReturnType;

                if (returnType.IsPrimitive)
                {
                    _image.AddTypeRef(returnType, out int returnTypeMetadataToken);

                    instructions.Add(OpCodes.Box, returnTypeMetadataToken);

                    if (returnType.IsPrimitive || returnType.IsEnum)
                        retInstruction = new Instruction(OpCodes.Unbox_Any, returnTypeMetadataToken);
                }
            }

            instructions.Add(OpCodes.Callvirt, setResultMetadataToken);
            instructions.Add(OpCodes.Ldloc_S, callManagerInstanceIndex);
            instructions.Add(OpCodes.Callvirt, releaseTaskMetadataToken);
            instructions.Add(OpCodes.Callvirt, getResultMetadataToken);

            if (retInstruction != null)
                instructions.Add(retInstruction);

            instructions.Add(OpCodes.Ret);

            byte[] il = instructions;

            MethodBody body = new(il, _method.Module)
            {
                LocalVariables = localVariables
            };

            return body;
        }

        public void Dispose()
        {
            _imageReader?.Dispose();
        }
    }
}