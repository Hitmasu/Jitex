using System;
using System.Collections.Generic;
using System.Linq;
using Jitex.PE;
using System.Reflection;
using Jitex.Builder.IL;
using Jitex.Internal;
using Jitex.Utils;
using Microsoft.Extensions.Logging;
using LocalVariableInfo = Jitex.Builder.Method.LocalVariableInfo;
using MethodBody = Jitex.Builder.Method.MethodBody;
using Pointer = Jitex.Utils.Pointer;
using static System.Reflection.Emit.OpCodes;

namespace Jitex.Intercept
{
    internal class InterceptorBuilder : IDisposable
    {
        private static readonly ConstructorInfo CallContextCtor = typeof(CallContext).GetConstructor(new[] {typeof(long), typeof(Type[]), typeof(Type[]), typeof(Pointer), typeof(Pointer), typeof(Pointer[])})!;
        private static readonly ConstructorInfo CallManagerCtor = typeof(CallManager).GetConstructor(new[] {typeof(CallContext)})!;

        private static readonly MethodInfo CallInterceptors = typeof(CallManager).GetMethod(nameof(CallManager.CallInterceptors), BindingFlags.Public | BindingFlags.Instance)!;
        private static readonly MethodInfo ReleaseTask = typeof(CallManager).GetMethod(nameof(CallManager.ReleaseTask), BindingFlags.Public | BindingFlags.Instance)!;
        private static readonly MethodInfo PointerBox = typeof(Pointer).GetMethod(nameof(Pointer.Box))!;
        private static readonly MethodInfo GetProceedCall = typeof(CallContext).GetProperty(nameof(CallContext.ProceedCall))!.GetGetMethod()!;
        private static readonly MethodInfo GetReturnValue = typeof(CallContext).GetMethods(BindingFlags.Public | BindingFlags.Instance).First(w => w.Name == nameof(CallContext.GetReturnValue) && w.IsGenericMethod);
        private static readonly MethodInfo GetReturnValuePointer = typeof(CallManager).GetMethod(nameof(CallManager.GetReturnValuePointer), BindingFlags.Public | BindingFlags.Instance)!;
        private static readonly MethodInfo GetReturnValueNoRef = typeof(CallManager).GetMethod(nameof(CallManager.GetReturnValueNoRef), BindingFlags.Public | BindingFlags.Instance)!;
        private static readonly MethodInfo GetTypeFromHandle = GetTypeFromHandle = typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle))!;

        private readonly MethodBase _method;
        private readonly MethodBody _body;

        private ImageReader? _imageReader;
        private ImageInfo? _image;

        static InterceptorBuilder()
        {
            if (!JitexManager.ModuleIsLoaded<InternalModule>())
                JitexManager.LoadModule(InternalModule.Instance);
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
        ///     CallContext context = new CallContext(methodHandle, Pointer.Box((void*)this), Pointer.Box((void*)n1),Pointer.Box((void*)n2));
        ///     CallManager callManager = new CallManager(context);
        ///
        ///     callManager.CallInteceptorsAsync();
        ///
        ///     if(context.ProceedCall){
        ///         int result = n1+n2;
        ///         context.SetResult(Pointer.Box((void*)result);
        ///     }
        ///
        ///     callManager.ReleaseTask();
        ///
        ///     return context.GetResult<int>();
        /// }
        /// </code>
        /// </remarks>
        /// <returns></returns>
        public MethodBody InjectInterceptor(bool reuseReferences)
        {
            if (_imageReader == null)
            {
                _imageReader = new(_method.Module);
                _image = _imageReader.LoadImage(reuseReferences);
            }

            IList<LocalVariableInfo> localVariables = _body.LocalVariables;

            localVariables.Add(new LocalVariableInfo(typeof(CallContext)));
            byte callContextVariableIndex = (byte) (localVariables.Count - 1);

            localVariables.Add(new LocalVariableInfo(typeof(CallManager)));
            byte callManagerVariableIndex = (byte) (localVariables.Count - 1);

            _image!.AddOrGetMemberRef(CallContextCtor, out int callContextCtorMetadataToken);
            _image!.AddOrGetMemberRef(CallManagerCtor, out int callManagerCtorMetadataToken);
            _image!.AddOrGetMemberRef(CallInterceptors, out int callInterceptorMetadataToken);
            _image!.AddOrGetMemberRef(GetProceedCall, out int getProceedCallMetadataToken);
            _image!.AddOrGetMemberRef(ReleaseTask, out int releaseTaskMetadataToken);

            Instructions instructions = new();
            long methodHandle = MethodHelper.GetMethodHandle(_method).Value.ToInt64();
            instructions.Add(Ldc_I8, methodHandle);

            WriteGenericArguments(instructions);

            Type returnType;

            if (_method is MethodInfo mi)
            {
                if (MethodHelper.HasCanon(mi))
                    returnType = mi.GetGenericMethodDefinition().ReturnType;
                else
                    returnType = mi.ReturnType;
            }
            else
            {
                returnType = typeof(void);
            }

            byte ldargIndex = WriteInstanceParameter(instructions);
            byte? returnVariableIndex = WriteReturnVariable(localVariables, instructions, returnType);

            WriteParameters(instructions, ldargIndex);

            instructions.Add(Newobj, callContextCtorMetadataToken);
            instructions.Add(Stloc_S, callContextVariableIndex);
            instructions.Add(Ldloc_S, callContextVariableIndex);

            instructions.Add(Newobj, callManagerCtorMetadataToken);
            instructions.Add(Stloc_S, callManagerVariableIndex);
            instructions.Add(Ldloc_S, callManagerVariableIndex);

            instructions.Add(Callvirt, callInterceptorMetadataToken);

            instructions.Add(Ldloc_S, callContextVariableIndex);
            instructions.Add(Callvirt, getProceedCallMetadataToken);
            Instruction gotoInstruction = instructions.Add(Brfalse, 0); //if(context.ProceedCall)

            instructions.AddRange(_body.ReadIL());
            instructions.RemoveLast(); //Remove Ret instruction.

            if (returnType != typeof(void))
                instructions.Add(Stloc_S, returnVariableIndex);

            Instruction endpointGoto = instructions.Add(Ldloc_S, callManagerVariableIndex);
            instructions.Add(Callvirt, releaseTaskMetadataToken);
            gotoInstruction.Value = (endpointGoto.Offset - gotoInstruction.Offset - gotoInstruction.Size);

            WriteGetReturnValue(instructions, callContextVariableIndex, callManagerVariableIndex, returnType);

            instructions.Add(Ret);

            byte[] il = instructions;

            MethodBody body = new(il, _method.Module)
            {
                LocalVariables = localVariables
            };

            return body;
        }

        /// <summary>
        /// Write instructions to load parameters from method.
        /// </summary>
        /// <param name="instructions"></param>
        /// <param name="ldargIndex"></param>
        private void WriteParameters(Instructions instructions, byte ldargIndex)
        {
            _image!.AddOrGetTypeRef(typeof(Pointer), out int pointerTypeMetadataToken);
            _image!.AddOrGetMemberRef(PointerBox, out int pointerBoxMetadataToken);

            Type[] parameters = _method.GetParameters().Select(w => w.ParameterType).ToArray();

            instructions.Add(Ldc_I4_S, (byte) parameters.Length);
            instructions.Add(Newarr, pointerTypeMetadataToken);

            for (byte i = 0; i < parameters.Length; i++)
            {
                Type parameter = parameters[i];

                instructions.Add(Dup);
                instructions.Add(Ldc_I4_S, i);

                instructions.Add(Ldarga_S, ldargIndex++);

                //TODO: Maybe set variable as pinned too?
                if (parameter.IsPointer)
                {
                    instructions.Add(Call, pointerBoxMetadataToken);
                }
                else
                {
                    instructions.Add(Conv_U);
                    instructions.Add(Call, pointerBoxMetadataToken);
                }

                instructions.Add(Stelem_Ref);
            }
        }

        /// <summary>
        /// Write instructions to load instance.
        /// </summary>
        /// <param name="instructions"></param>
        /// <returns></returns>
        private byte WriteInstanceParameter(Instructions instructions)
        {
            if (_method.IsStatic)
            {
                instructions.Add(Ldnull);
                return 0;
            }

            _image!.AddOrGetMemberRef(PointerBox, out int pointerBoxMetadataToken);

            instructions.Add(Ldarga_S, (byte) 0);
            instructions.Add(Conv_U);
            instructions.Add(Call, pointerBoxMetadataToken);
            return 1;
        }

        /// <summary>
        /// Create a return variable and write instructions to load him. 
        /// </summary>
        /// <param name="variables"></param>
        /// <param name="instructions"></param>
        /// <param name="returnType"></param>
        /// <returns></returns>
        private byte? WriteReturnVariable(IList<LocalVariableInfo> variables, Instructions instructions, Type returnType)
        {
            if (returnType == typeof(void))
            {
                instructions.Add(Ldnull);
                return null;
            }

            _image!.AddOrGetMemberRef(PointerBox, out int pointerBoxMetadataToken);

            variables.Add(new LocalVariableInfo(returnType));
            byte returnVariableIndex = (byte) (variables.Count - 1);

            instructions.Add(Ldloca_S, returnVariableIndex);

            if (returnType.IsPointer)
            {
                instructions.Add(Call, pointerBoxMetadataToken);
            }
            else
            {
                instructions.Add(Conv_U);
                instructions.Add(Call, pointerBoxMetadataToken);
            }

            return returnVariableIndex;
        }

        /// <summary>
        /// Write instructions to get return value.
        /// </summary>
        /// <param name="instructions"></param>
        /// <param name="callContextVariableIndex"></param>
        /// <param name="callManagerVariableIndex"></param>
        /// <param name="returnType"></param>
        private void WriteGetReturnValue(Instructions instructions, byte callContextVariableIndex, byte callManagerVariableIndex, Type returnType)
        {
            if (returnType == typeof(void))
                return;

            MethodInfo getResult;

            if (returnType.IsByRef)
            {
                instructions.Add(Ldloc_S, callContextVariableIndex);
                instructions.Add(Ldc_I4_1);
                getResult = GetReturnValue.MakeGenericMethod(returnType.GetElementType());
            }
            else if (returnType.IsPointer)
            {
                instructions.Add(Ldloc_S, callContextVariableIndex);
                getResult = GetReturnValuePointer;
            }
            else
            {
                instructions.Add(Ldloc_S, callManagerVariableIndex);
                getResult = GetReturnValueNoRef.MakeGenericMethod(returnType);
            }

            _image!.AddOrGetMemberRef(getResult, out int getResultMetadataToken);
            instructions.Add(Callvirt, getResultMetadataToken);
        }

        /// <summary>
        /// Write instructions to load generic arguments
        /// </summary>
        /// <remarks>
        /// <code>
        /// class Type<T1,T2>{}
        /// void Method<TM1,TM2>(){}
        /// Type[] genericTypeMethods = {typeof(T1),typeof(T2)};
        /// Type[] genericTypeMethods = {typeof(TM1),typeof(TM2)};
        /// </code>
        /// </remarks>
        /// <param name="instructions"></param>
        private void WriteGenericArguments(Instructions instructions)
        {
            if (TypeHelper.HasCanon(_method.DeclaringType))
            {
                Type[] types = _method.DeclaringType!.GetGenericTypeDefinition().GetGenericArguments();
                WriteTypesOnArray(instructions, types);
            }
            else
            {
                instructions.Add(Ldnull);
            }

            if (MethodHelper.HasCanon(_method, false))
            {
                MethodInfo methodInfo = (MethodInfo) _method;
                Type[] types = methodInfo.GetGenericMethodDefinition().GetGenericArguments();
                WriteTypesOnArray(instructions, types);

                InternalModule.Instance.LoadMethodSpec(methodInfo);
            }
            else
            {
                instructions.Add(Ldnull);
            }
        }

        /// <summary>
        /// Write instructions to load generic argument.
        /// </summary>
        /// <param name="instructions"></param>
        /// <param name="types"></param>
        private void WriteTypesOnArray(Instructions instructions, IReadOnlyCollection<Type> types)
        {
            ValidateImageLoaded();

            _image!.AddOrGetTypeRef(typeof(Type), out int typeMetadataToken);
            _image!.AddOrGetMemberRef(GetTypeFromHandle, out int getTypeFromHandleMetadataToken);

            instructions.Add(Ldc_I4_S, (byte) types.Count);
            instructions.Add(Newarr, typeMetadataToken);

            for (byte i = 0; i < types.Count; i++)
            {
                int ldToken = MetadataTokenBase.TypeSpec + i;

                instructions.Add(Dup);
                instructions.Add(Ldc_I4_S, i);
                instructions.Add(Ldtoken, ldToken);
                instructions.Add(Call, getTypeFromHandleMetadataToken);
                instructions.Add(Stelem_Ref);
            }
        }

        private void ValidateImageLoaded()
        {
            if (_image == null)
                throw new InvalidOperationException("Image not loaded!");
        }

        public void Dispose()
        {
            _imageReader?.Dispose();
        }
    }
}