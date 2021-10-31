using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Iced.Intel;
using Jitex.Runtime;
using Jitex.Utils;
using Jitex.Utils.Extension;

namespace Jitex.Intercept
{
    /// <summary>
    /// Prepare a method to intercept call.
    /// </summary>
    internal class InterceptBuilder
    {
        private readonly TypeBuilder _interceptorTypeBuilder;
        private readonly MethodBase _method;
        private readonly bool _hasReturn;
        private readonly IReadOnlyCollection<ParameterType> _parameters;
        private readonly bool _hasCanon;
        private static ulong _methodAccessExceptionAddress;
        private static MethodInfo _firstMethodValidation;

        private static readonly Type CanonType;
        private static readonly MethodInfo InterceptCallAsync;
        private static readonly MethodInfo InterceptAsyncCallAsync;
        private static readonly MethodInfo CompletedTask;
        private static readonly MethodInfo CallDispose;
        private static readonly MethodInfo GetReferenceFromTypedReference;
        private static readonly ConstructorInfo ConstructorObject;
        private static readonly ConstructorInfo ConstructorCallManager;
        private static readonly ConstructorInfo ConstructorIntPtrLong;

        static InterceptBuilder()
        {
            CanonType = Type.GetType("System.__Canon");
            ConstructorObject = typeof(object).GetConstructor(Type.EmptyTypes)!;
            CompletedTask = typeof(Task).GetProperty(nameof(Task.CompletedTask))!.GetGetMethod();
            ConstructorCallManager = typeof(CallManager).GetConstructors().First();
            InterceptCallAsync = typeof(CallManager).GetMethods(BindingFlags.Public | BindingFlags.Instance).First(w => w.Name == nameof(CallManager.InterceptCallAsync) && !w.IsGenericMethod);
            InterceptAsyncCallAsync = typeof(CallManager).GetMethods(BindingFlags.Public | BindingFlags.Instance).First(w => w.Name == nameof(CallManager.InterceptCallAsync) && w.IsGenericMethod);
            ConstructorIntPtrLong = typeof(IntPtr).GetConstructor(new[] { typeof(long) })!;
            CallDispose = typeof(CallManager).GetMethod(nameof(CallManager.Dispose), BindingFlags.Public | BindingFlags.Instance)!;
            GetReferenceFromTypedReference = typeof(MarshalHelper).GetMethod(nameof(MarshalHelper.GetReferenceFromTypedReference))!;
        }

        /// <summary>
        /// Create a builder
        /// </summary>
        /// <param name="method">Method original which will be intercept.</param>
        public InterceptBuilder(MethodBase method)
        {
            _method = method;

            AssemblyName assemblyName = new AssemblyName($"{_method.Module.Assembly.GetName().Name}_{_method.Name}Jitex");
            AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule($"{_method.Module.Name}.{_method.Name}.Jitex");

            _interceptorTypeBuilder = moduleBuilder.DefineType($"{_method.DeclaringType.Name}.{_method.Name}.Jitex");

            if (method is MethodInfo methodInfo)
                _hasReturn = methodInfo.ReturnType != typeof(Task) && methodInfo.ReturnType != typeof(ValueTask) && methodInfo.ReturnType != typeof(void);
            else
                _hasReturn = false;

            _hasCanon = TypeHelper.HasCanon(_method.DeclaringType) || MethodHelper.HasCanon(_method);
            _parameters = BuildParameterType().ToList();
        }

        /// <summary>
        /// Create a method to intercept.
        /// </summary>
        /// <returns></returns>
        public MethodBase Create()
        {
            MethodInfo interceptor = CreateMethodInterceptor();
            RemoveAccessValidation(interceptor);
            return interceptor;
        }

        private MethodInfo CreateMethodInterceptor()
        {
            string methodName = $"{_method.Name}Jitex";
            MethodInfo methodInfo = (MethodInfo)_method;

            MethodAttributes methodAttributes = MethodAttributes.Public;

            if (_method.IsStatic)
                methodAttributes |= MethodAttributes.Static;

            MethodBuilder builder = _interceptorTypeBuilder.DefineMethod(methodName, methodAttributes,
                CallingConventions.Standard, methodInfo.ReturnType, _parameters.Select(w => w.Type).ToArray());

            ILGenerator generator = builder.GetILGenerator();
            BuildBody(generator, methodInfo.ReturnType);

            TypeInfo type = _interceptorTypeBuilder.CreateTypeInfo();

            return type.GetMethod(methodName)!;
        }


        /// <summary>
        /// Create the body of method interceptor.
        /// </summary>
        /// <param name="generator">Generator of method.</param>
        /// <param name="returnType">Return type of method.</param>
        /// <remarks>
        /// Thats just create a middleware to call InterceptCall.
        /// Basically, that will be generated:
        /// public ReturnType Method(args){
        ///    object[] args = new object[args.length];
        ///    object[0] = this
        ///    object[1] = args[1]
        ///    ....
        ///    return InterceptCall();
        /// }
        /// </remarks>
        private void BuildBody(ILGenerator generator, Type returnType)
        {
            bool isAwaitable = _method.IsAwaitable();
            int totalArgs = _parameters.Count;

            if (_method.IsConstructor && !_method.IsStatic)
            {
                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Call, ConstructorObject);
            }

            generator.DeclareLocal(typeof(object[]));
            generator.Emit(OpCodes.Ldc_I4, totalArgs);
            generator.Emit(OpCodes.Newarr, typeof(object));
            generator.Emit(OpCodes.Stloc_0);

            if (totalArgs > 0)
            {
                int argIndex = 0;

                foreach (ParameterType parameterType in _parameters)
                {
                    generator.Emit(OpCodes.Ldloc_0);
                    generator.Emit(OpCodes.Ldc_I4, argIndex);

                    if (parameterType.OriginalType is { IsByRef: true })
                        generator.Emit(OpCodes.Ldarg_S, argIndex);
                    else
                        generator.Emit(OpCodes.Ldarga_S, argIndex);

                    generator.Emit(OpCodes.Mkrefany, parameterType.Type);
                    generator.Emit(OpCodes.Call, GetReferenceFromTypedReference);
                    generator.Emit(OpCodes.Box, typeof(IntPtr));
                    generator.Emit(OpCodes.Stelem_Ref);

                    argIndex++;
                }
            }

            Type returnTypeInterceptor;

            if (returnType.IsPointer || returnType.IsByRef || returnType == typeof(void) || returnType == CanonType)
                returnTypeInterceptor = typeof(IntPtr);
            else if (isAwaitable && returnType.IsGenericType)
                returnTypeInterceptor = returnType.GetGenericArguments().First();
            else if (returnType.IsPrimitive || returnType.IsValueType)
                returnTypeInterceptor = returnType;
            else
                returnTypeInterceptor = typeof(IntPtr);

            MethodInfo getAwaiter = typeof(Task<>).MakeGenericType(returnTypeInterceptor).GetMethod(nameof(Task.GetAwaiter), BindingFlags.Public | BindingFlags.Instance)!;
            MethodInfo getResult = typeof(TaskAwaiter<>).MakeGenericType(returnTypeInterceptor).GetMethod(nameof(TaskAwaiter.GetResult), BindingFlags.Public | BindingFlags.Instance)!;
            LocalBuilder awaiterVariable = generator.DeclareLocal(typeof(TaskAwaiter<>).MakeGenericType(returnTypeInterceptor));

            IntPtr methodHandle = MethodHelper.GetMethodHandle(_method).Value;
            generator.Emit(OpCodes.Ldc_I8, methodHandle.ToInt64());
            generator.Emit(OpCodes.Newobj, ConstructorIntPtrLong);

            generator.Emit(OpCodes.Ldloca_S, 0);

            if (_hasCanon)
                generator.Emit(OpCodes.Ldc_I4_1);
            else
                generator.Emit(OpCodes.Ldc_I4_0);

            if (_method.IsGenericMethod)
                generator.Emit(OpCodes.Ldc_I4_1);
            else
                generator.Emit(OpCodes.Ldc_I4_0);

            if (_method.IsStatic)
                generator.Emit(OpCodes.Ldc_I4_1);
            else
                generator.Emit(OpCodes.Ldc_I4_0);

            generator.Emit(OpCodes.Newobj, ConstructorCallManager);
            generator.Emit(OpCodes.Dup);

            if ((isAwaitable && returnType.IsGenericType || returnType.CanBeInline()))
            {
                MethodInfo interceptor = InterceptAsyncCallAsync.MakeGenericMethod(returnTypeInterceptor);
                _firstMethodValidation = interceptor;
                generator.Emit(OpCodes.Call, interceptor);
            }
            else
            {
                _firstMethodValidation = getAwaiter;
                generator.Emit(OpCodes.Call, InterceptCallAsync);
            }

            generator.Emit(OpCodes.Call, getAwaiter);
            generator.Emit(OpCodes.Stloc, awaiterVariable.LocalIndex);
            generator.Emit(OpCodes.Ldloca_S, awaiterVariable.LocalIndex);
            generator.Emit(OpCodes.Call, getResult);

            if (_hasReturn)
            {
                LocalBuilder retVariable;

                if (isAwaitable)
                {
                    retVariable = generator.DeclareLocal(returnTypeInterceptor);

                    generator.Emit(OpCodes.Stloc_S, retVariable.LocalIndex);
                    generator.Emit(OpCodes.Call, CallDispose);
                    generator.Emit(OpCodes.Ldloc_S, retVariable.LocalIndex);

                    if (returnType.IsTask())
                    {
                        MethodInfo fromResult = typeof(Task).GetMethod(nameof(Task.FromResult))!.MakeGenericMethod(returnTypeInterceptor);
                        generator.Emit(OpCodes.Call, fromResult);
                    }
                    else
                    {
                        ConstructorInfo ctorValueTask = returnType.GetConstructor(new[] { returnTypeInterceptor })!;
                        generator.Emit(OpCodes.Newobj, ctorValueTask);
                    }
                }
                else
                {
                    retVariable = generator.DeclareLocal(returnTypeInterceptor);
                    generator.Emit(OpCodes.Stloc_S, retVariable.LocalIndex);
                    generator.Emit(OpCodes.Call, CallDispose);
                    generator.Emit(OpCodes.Ldloc_S, retVariable.LocalIndex);
                }
            }
            else
            {
                generator.Emit(OpCodes.Pop);
                generator.Emit(OpCodes.Call, CallDispose);

                if (isAwaitable)
                {
                    if (returnType.IsTask())
                    {
                        generator.Emit(OpCodes.Call, CompletedTask);
                    }
                    else
                    {
                        LocalBuilder defaultTaskVariable = generator.DeclareLocal(typeof(ValueTask));
                        generator.Emit(OpCodes.Ldloca_S, defaultTaskVariable.LocalIndex);
                        generator.Emit(OpCodes.Initobj, typeof(ValueTask));
                        generator.Emit(OpCodes.Ldloc, defaultTaskVariable.LocalIndex);
                    }
                }
            }

            generator.Emit(OpCodes.Ret);
        }

        private IEnumerable<ParameterType> BuildParameterType()
        {
            if (!_method.IsStatic)
                yield return new ParameterType(null, typeof(IntPtr));

            if (_hasCanon)
                yield return new ParameterType(typeof(IntPtr).MakeByRefType(), typeof(IntPtr));

            IReadOnlyCollection<Type> parameters = _method.GetParameters().Select(w => w.ParameterType).ToList();

            foreach (Type parameterType in parameters)
            {
                if (parameterType.IsPrimitive)
                    yield return new ParameterType(parameterType, parameterType);
                else
                    yield return new ParameterType(parameterType, typeof(IntPtr));
            }
        }

        /// <summary>
        /// Remove access validation from code.
        /// </summary>
        /// <remarks>
        /// When a method is compiled, a lot of validation is added on native code. As MethodBuilder doesn't have skipVisibility like DynamicMethod, we need
        /// remove those validation to gain access on not public class/struct.
        /// ----
        /// 
        /// Example of remove:
        /// 0x00007ff7f9029eeb 48 b9 78 5a 1f f9 f7 7f 00 00 mov    rcx, 7FF7F91F5A78h
        /// 0x00007ff7f9029ef5 48 ba 18 11 1f f9 f7 7f 00 00 mov    rdx, 7FF7F91F1118h
        /// 0x00007ff7f9029eff e8 dc 0a ca 5f                       call    coreclr!JIT_ThrowMethodAccessException (0x00007ff858cca9e0)
        /// 0x00007ff7f9029f04 48 8b 4d b0                          mov     rcx, qword ptr [rbp-50h]
        ///
        /// Will be came:
        /// 0x00007ff7f9029eeb EB 17                                jmp
        /// [ignored] 0x00007ff7f9029ef5 - 0x00007ff7f9029eff
        /// 0x00007ff7f9029f04 48 8b 4d b0                          mov     rcx, qword ptr [rbp-50h]
        /// 
        /// </remarks>
        /// <param name="origin">Original method.</param>
        /// <param name="dest">Method validation.</param>
        ///
        /// --- Destination method should be the first method validated on native code.
        private void RemoveAccessValidation(MethodBase origin)
        {
            try
            {
                CancellationTokenSource source = new CancellationTokenSource(TimeSpan.FromSeconds(1));
                NativeCode native = RuntimeMethodCache.GetNativeCodeAsync(origin, source.Token).GetAwaiter().GetResult();

                byte[] nativeCode = new byte[native.Size];
                Marshal.Copy(native.Address, nativeCode, 0, native.Size);

                ByteArrayCodeReader codeReader = new ByteArrayCodeReader(nativeCode);

                Decoder decoder = Decoder.Create(64, codeReader, (ulong) native.Address.ToInt64());
                ulong endIp = decoder.IP + (ulong) native.Size;

                ulong handleOrigin = (ulong) MethodHelper.GetMethodHandle(origin).Value.ToInt64();
                ulong handleDest = (ulong) MethodHelper.GetMethodHandle(_firstMethodValidation).Value.ToInt64();

                //mov register methodHandle (have 10 bytes = 1 instruction | 1 register | 8 address)
                //mov register methodHandle (have 10 bytes = 1 instruction | 1 register | 8 address)
                const int movSize = 20;

                //mov + call MethodAccessException
                const int callSize = movSize + 5;

                do
                {
                    bool writeAddress = false;
                    IntPtr startInstruction = IntPtr.Zero;

                    Instruction instruction = decoder.Decode();

                    if (_methodAccessExceptionAddress == 0 && instruction.Mnemonic == Mnemonic.Mov && instruction.Immediate64 == handleOrigin)
                    {
                        Instruction nextInstruction = decoder.Decode();

                        if (nextInstruction.Mnemonic == Mnemonic.Mov && nextInstruction.Immediate64 == handleDest)
                        {
                            Instruction callInstruction = decoder.Decode();

                            if (callInstruction.Mnemonic != Mnemonic.Call)
                                continue;

                            _methodAccessExceptionAddress = callInstruction.Immediate64;
                            startInstruction = new IntPtr((long) instruction.IP);
                            writeAddress = true;
                        }
                    }
                    else if (instruction.Mnemonic == Mnemonic.Call && instruction.Immediate64 == _methodAccessExceptionAddress && instruction.Immediate64 != 0)
                    {
                        startInstruction = new IntPtr((long) instruction.IP - movSize);
                        writeAddress = true;
                    }

                    if (writeAddress)
                    {
                        byte[] nopInstructions = new byte[2];
                        nopInstructions[0] = 0xEB; //jmp near short
                        nopInstructions[1] = callSize - 2;

                        Marshal.Copy(nopInstructions, 0, startInstruction, nopInstructions.Length);
                    }
                } while (decoder.IP < endIp);
            }
            catch
            {
                //...
            }
        }
    }
}