using System;
using System.Collections.Generic;
using System.Linq;
using Jitex.PE;
using System.Reflection;
using System.Reflection.Emit;
using Jitex.Builder.IL;
using Jitex.Utils;
using Jitex.Utils.Extension;
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
        private static readonly MethodInfo ReleaseTask;
        private static readonly MethodInfo PointerBox;
        private static readonly MethodInfo GetProceedCall;
        private static readonly MethodInfo GetResult;

        private readonly MethodBase _method;
        private readonly MethodBody _body;

        private ImageReader? _imageReader;
        private ImageInfo? _image;

        static InterceptorBuilder()
        {
            CallContextCtor = typeof(CallContext).GetConstructor(new[] {typeof(long), typeof(object), typeof(object[])})!;
            CallManagerCtor = typeof(CallManager).GetConstructor(new[] {typeof(CallContext)})!;
            CallInterceptors = typeof(CallManager).GetMethod(nameof(CallManager.CallInterceptors), BindingFlags.Public | BindingFlags.Instance)!;
            ReleaseTask = typeof(CallManager).GetMethod(nameof(CallManager.ReleaseTask), BindingFlags.Public | BindingFlags.Instance)!;
            PointerBox = typeof(Pointer).GetMethod(nameof(Pointer.Box))!;
            GetResult = typeof(CallContext).GetMethod(nameof(CallContext.GetReturnValue), BindingFlags.Public | BindingFlags.Instance)!;
            SetResult = typeof(CallContext).GetMethod(nameof(CallContext.SetReturnValue), BindingFlags.Public | BindingFlags.Instance)!;

            PropertyInfo proceedCall = typeof(CallContext).GetProperty(nameof(CallContext.ProceedCall))!;
            GetProceedCall = proceedCall.GetGetMethod();
        }

        public InterceptorBuilder(MethodBase method, MethodBody body)
        {
            _method = method;
            _body = body;
        }

        /// <summary>
        /// Inject interceptor calls on body of method.
        /// </summary>
        /// <remarks>
        /// Inject the follow code:
        /// <code>
        /// public static int Sum(int n1, int n2) => n1+n2;
        /// </code>
        /// Becomes this:
        /// <code>
        /// public int Sum(int n1, int n2){
        ///     CallContext context = new CallContext(this,n1,n2);
        ///     CallManager callManager = new CallManager(context);
        ///
        ///     callManager.CallInteceptorsAsync();
        ///
        ///     if(context.ProceedCall)
        ///         context.SetResult<int>(n1+n2);
        ///
        ///     callManager.ReleaseTask();
        ///
        ///     return context.GetResult<int>();
        /// }
        /// </code>
        /// </remarks>
        /// <returns></returns>
        public MethodBody InjectInterceptor()
        {
            if (_imageReader == null)
            {
                _imageReader = new(_method.Module);
                _image = _imageReader.LoadImage();
            }

            IList<Type> parameters = new List<Type>(_method.GetParameters().Select(s => s.ParameterType));

            IList<LocalVariableInfo> localVariables = _body.LocalVariables ?? new List<LocalVariableInfo>();

            localVariables.Add(new LocalVariableInfo(typeof(CallContext)));
            int callContextVariableIndex = localVariables.Count - 1;

            localVariables.Add(new LocalVariableInfo(typeof(CallManager)));
            int callManagerVaraibleIndex = localVariables.Count - 1;

            _image!.AddTypeRef(typeof(object), out int objCtorMetadataToken);
            _image!.AddMemberRef(CallContextCtor, out int callContextCtorMetadataToken);
            _image!.AddMemberRef(CallManagerCtor, out int callManagerCtorMetadataToken);
            _image!.AddMemberRef(CallInterceptors, out int callInterceptorMetadataToken);
            _image!.AddMemberRef(GetProceedCall, out int getProceedCallMetadataToken);
            _image!.AddMemberRef(ReleaseTask, out int releaseTaskMetadataToken);
            _image!.AddMemberRef(PointerBox, out int pointerBoxMetadataToken);


            Instructions instructions = new();
            long methodHandle = MethodHelper.GetMethodHandle(_method).Value.ToInt64();
            instructions.Add(OpCodes.Ldc_I8, methodHandle);

            MethodInfo? methodInfo = _method as MethodInfo;
            int ldargIndex = 0;

            if (methodInfo != null && !methodInfo.IsStatic)
            {
                instructions.Add(OpCodes.Ldarg_0);
                ldargIndex = 1;
            }
            else
            {
                instructions.Add(OpCodes.Ldnull);
            }

            instructions.Add(OpCodes.Ldc_I4_S, parameters.Count);
            instructions.Add(OpCodes.Newarr, objCtorMetadataToken);

            for (int i = 0; i < parameters.Count; i++)
            {
                Type parameter = parameters[i];

                instructions.Add(OpCodes.Dup);
                instructions.Add(OpCodes.Ldc_I4_S, i);

                if (parameter.IsByRef || parameter.IsPointer)
                    instructions.Add(OpCodes.Ldarg_S, ldargIndex++);
                else
                    instructions.Add(OpCodes.Ldarga_S, ldargIndex++);

                //TODO: Maybe set variable as pinned too?
                if (parameter.IsPointer)
                {
                    instructions.Add(OpCodes.Call, pointerBoxMetadataToken);
                }
                else
                {
                    instructions.Add(OpCodes.Conv_U);
                    instructions.Add(OpCodes.Call, pointerBoxMetadataToken);
                }

                instructions.Add(OpCodes.Stelem_Ref);
            }

            instructions.Add(OpCodes.Newobj, callContextCtorMetadataToken);
            instructions.Add(OpCodes.Stloc_S, callContextVariableIndex);
            instructions.Add(OpCodes.Ldloc_S, callContextVariableIndex);

            instructions.Add(OpCodes.Newobj, callManagerCtorMetadataToken);
            instructions.Add(OpCodes.Stloc_S, callManagerVaraibleIndex);
            instructions.Add(OpCodes.Ldloc_S, callManagerVaraibleIndex);

            instructions.Add(OpCodes.Callvirt, callInterceptorMetadataToken);

            instructions.Add(OpCodes.Ldloc_S, callContextVariableIndex);
            instructions.Add(OpCodes.Callvirt, getProceedCallMetadataToken);
            Instruction gotoInstruction = instructions.Add(OpCodes.Brfalse_S, (byte) 0x00); //if(context.ProceedCall)

            instructions.AddRange(_body.ReadIL());
            instructions.RemoveLast(); //Remove Ret instruction.

            if (methodInfo != null)
            {
                Type returnType = methodInfo.ReturnType;

                if (returnType != typeof(void))
                {
                    localVariables.Add(new LocalVariableInfo(returnType));
                    int returnVariableIndex = localVariables.Count - 1;

                    MethodInfo setResult = SetResult.MakeGenericMethod(returnType);
                    _image.AddMethodSpec(setResult, out int setResultMetadataToken);

                    instructions.Add(OpCodes.Stloc_S, returnVariableIndex);
                    instructions.Add(OpCodes.Ldloc_S, callContextVariableIndex);
                    instructions.Add(OpCodes.Ldloc_S, returnVariableIndex);
                    instructions.Add(OpCodes.Callvirt, setResultMetadataToken);
                }
            }

            Instruction endpointGoto =  instructions.Add(OpCodes.Ldloc_S, callManagerVaraibleIndex);
            instructions.Add(OpCodes.Callvirt, releaseTaskMetadataToken);
            gotoInstruction.Value = (byte) (endpointGoto.Offset - gotoInstruction.Offset - gotoInstruction.Size);

            if (methodInfo != null)
            {
                Type returnType = methodInfo.ReturnType;

                if (returnType != typeof(void))
                {
                    MethodInfo getResult = GetResult.MakeGenericMethod(returnType);

                    _image.AddMemberRef(getResult, out int getResultMetadataToken);
                    instructions.Add(OpCodes.Ldloc_S, callContextVariableIndex);
                    instructions.Add(OpCodes.Callvirt, getResultMetadataToken);
                }
            }

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