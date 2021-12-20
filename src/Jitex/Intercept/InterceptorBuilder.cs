using System;
using System.Collections.Generic;
using System.Linq;
using Jitex.PE;
using System.Reflection;
using System.Reflection.Emit;
using Jitex.Builder.IL;
using LocalVariableInfo = Jitex.Builder.Method.LocalVariableInfo;
using MethodBody = Jitex.Builder.Method.MethodBody;

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

        private readonly MethodBase _method;
        private readonly MethodBody _body;
        private readonly InternalModule _internalModule;
        private readonly TokenResolver _resolver;

        private ImageReader? _imageReader;
        private ImageInfo? _image;

        static InterceptorBuilder()
        {
            CallContextCtor = typeof(CallContext).GetConstructor(new[] {typeof(object[])})!;
            CallManagerCtor = typeof(CallManager).GetConstructor(new[] {typeof(CallContext)})!;
            CallInterceptors = typeof(CallManager).GetMethod(nameof(CallManager.CallInterceptors), BindingFlags.Public | BindingFlags.Instance)!;
            ReleaseTask = typeof(CallManager).GetMethod(nameof(CallManager.ReleaseTask), BindingFlags.Public | BindingFlags.Instance)!;

            PropertyInfo result = typeof(CallContext).GetProperty(nameof(CallContext.Result))!;
            GetResult = result.GetGetMethod();
            SetResult = result.GetSetMethod();
        }

        public InterceptorBuilder(MethodBase method, MethodBody body)
        {
            _method = method;
            _body = body;

            _internalModule = InitializeModule();
            _resolver = new TokenResolver();
        }

        private static InternalModule InitializeModule()
        {
            if (!JitexManager.TryGetModule(out InternalModule? instance))
            {
                instance = InternalModule.Instance;
                JitexManager.LoadModule(instance);
            }

            return instance!;
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

            IList<LocalVariableInfo>? localVariables = _body.LocalVariables ?? new List<LocalVariableInfo>();
            localVariables.Add(new(typeof(CallManager)));

            int callManagerInstanceIndex = localVariables.Count - 1;

            //TODO: Check if ctor/method was already implemented on module: if (!_imageReader.TryGetRefToken(ObjCtor, out int objCtorMetadataToken)

            _image!.AddTypeRef(typeof(object), out int objCtorMetadataToken);
            // int objCtorMetadataToken = _image!.GetNewTypeRefIndex();
            // AddNewToken(objCtorMetadataToken, typeof(object));

            _image!.AddMethodRef(CallContextCtor, out int callContextCtorMetadataToken);
            // int callContextCtorMetadataToken = _image!.GetNewMethodRefIndex();
            // AddNewToken(callContextCtorMetadataToken, CallContextCtor);

            _image!.AddMethodRef(CallManagerCtor, out int callManagerCtorMetadataToken);
            // int callManagerCtorMetadataToken = _image!.GetNewMethodRefIndex();
            // AddNewToken(callManagerCtorMetadataToken, CallManagerCtor);

            _image!.AddMethodRef(CallInterceptors, out int callInterceptorMetadataToken);
            // int callInterceptorMetadataToken = _image!.GetNewMethodRefIndex();
            // AddNewToken(callInterceptorMetadataToken, CallInterceptors);

            _image!.AddMethodRef(SetResult, out int setResultMetadataToken);
            // int setResultMetadataToken = _image!.GetNewMethodRefIndex();
            // AddNewToken(setResultMetadataToken, SetResult);

            _image!.AddMethodRef(GetResult, out int getResultMetadataToken);
            // int getResultMetadataToken = _image!.GetNewMethodRefIndex();
            // AddNewToken(getResultMetadataToken, GetResult);

            _image!.AddMethodRef(ReleaseTask, out int releaseTaskMetadataToken);
            // int releaseTaskMetadataToken = _image!.GetNewMethodRefIndex();
            // AddNewToken(releaseTaskMetadataToken, ReleaseTask);

            Instructions instructions = new();

            instructions.Add(OpCodes.Ldc_I4_S, parameters.Count);
            instructions.Add(OpCodes.Newarr, objCtorMetadataToken);

            for (int i = 0; i < parameters.Count; i++)
            {
                Type parameter = parameters[i];

                instructions.Add(OpCodes.Dup);
                instructions.Add(OpCodes.Ldc_I4_S, i);
                instructions.Add(OpCodes.Ldarg_S, i);

                if (parameter.IsValueType)
                {
                    instructions.Add(OpCodes.Box, parameter.MetadataToken);
                    AddNewToken(parameter.MetadataToken, parameter); //TODO: Check if token already exists
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
                    instructions.Add(OpCodes.Box, returnType.MetadataToken);
                    AddNewToken(returnType.MetadataToken, returnType);

                    retInstruction = new Instruction(OpCodes.Unbox_Any, returnType.MetadataToken);
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
                LocalVariables = localVariables,
                // CustomTokenResolver = _resolver
            };

            var ops = body.ReadIL();

            return body;
        }

        private static byte[] GetBytesFromToken(int token)
        {
            return new[]
            {
                (byte) (token & 0x000000FF),
                (byte) (token & 0x0000FF00),
                (byte) (token & 0x00FF0000),
                (byte) (token & 0xFF000000),
            };
        }

        private void AddNewToken(int metadataToken, MemberInfo method)
        {
            // _internalModule.AddTokenToResolution(_method, metadataToken, method);
            _resolver.AddToken(metadataToken, method);
        }

        public void Dispose()
        {
            _imageReader?.Dispose();
        }
    }
}