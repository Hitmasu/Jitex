﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Jitex.Utils;
using Jitex.Utils.Extension;
using IntPtr = System.IntPtr;
using MethodBody = Jitex.Builder.Method.MethodBody;

namespace Jitex.Intercept
{
    /// <summary>
    /// Prepare a method to intercept call.
    /// </summary>
    internal class InterceptBuilder
    {
        private readonly TypeBuilder _interceptorTypeBuilder;
        
        /// <summary>
        /// Method original.
        /// </summary>
        public MethodBase Method { get; }
        
        public bool HasReturn { get; }

        private static readonly MethodInfo InterceptCallAsync;
        private static readonly MethodInfo InterceptAsyncCallAsync;
        private static readonly MethodInfo CompletedTask;
        private static readonly MethodInfo CallDispose;
        private static readonly MethodInfo GetReferenceFromTypedReference;
        private static readonly ConstructorInfo ObjectCtor;
        private static readonly ConstructorInfo ConstructorCallManager;
        private static readonly ConstructorInfo ConstructorIntPtrLong;

        static InterceptBuilder()
        {
            ObjectCtor = typeof(object).GetConstructor(Type.EmptyTypes)!;
            CompletedTask = typeof(Task).GetProperty(nameof(Task.CompletedTask))!.GetGetMethod();
            ConstructorCallManager = typeof(CallManager).GetConstructor(new[] {typeof(IntPtr), typeof(object[]).MakeByRefType(), typeof(bool)})!;
            InterceptCallAsync = typeof(CallManager).GetMethods(BindingFlags.Public | BindingFlags.Instance).First(w => w.Name == nameof(CallManager.InterceptCallAsync) && !w.IsGenericMethod);
            InterceptAsyncCallAsync = typeof(CallManager).GetMethods(BindingFlags.Public | BindingFlags.Instance).First(w => w.Name == nameof(CallManager.InterceptCallAsync) && w.IsGenericMethod);
            ConstructorIntPtrLong = typeof(IntPtr).GetConstructor(new[] {typeof(long)})!;
            CallDispose = typeof(CallManager).GetMethod(nameof(CallManager.Dispose), BindingFlags.Public | BindingFlags.Instance)!;
            GetReferenceFromTypedReference = typeof(MarshalHelper).GetMethod(nameof(MarshalHelper.GetReferenceFromTypedReference))!;
        }

        /// <summary>
        /// Create a builder
        /// </summary>
        /// <param name="method">Method original which will be intercept.</param>
        public InterceptBuilder(MethodBase method)
        {
            Method = method;
            
            AssemblyName assemblyName = new AssemblyName($"{Method.Module.Assembly.GetName().Name}_{Method.Name}_Jitex");
            AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName,AssemblyBuilderAccess.Run);
            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule($"{Method.Module.Name}.{Method.Name}.Jitex");

            _interceptorTypeBuilder = moduleBuilder.DefineType($"{Method.DeclaringType.Name}.{Method.Name}.Jitex");

            if (method is MethodInfo methodInfo)
                HasReturn = methodInfo.ReturnType != typeof(Task) && methodInfo.ReturnType != typeof(ValueTask) && methodInfo.ReturnType != typeof(void);
            else
                HasReturn = false;
        }

        /// <summary>
        /// Create a method to intercept.
        /// </summary>
        /// <returns></returns>
        public MethodBase Create()
        {            
            return CreateMethodInterceptor();
        }

        private MethodInfo CreateMethodInterceptor()
        {
            string methodName = $"{Method.Name}Jitex";
            MethodInfo methodInfo = (MethodInfo) Method;
            List<Type> parameters = new List<Type>();
            MethodAttributes methodAttributes = MethodAttributes.Public;

            if (Method.IsGenericMethod)
                parameters.Add(typeof(IntPtr));

            if (!Method.IsStatic)
                parameters.Add(typeof(IntPtr));
            else
                methodAttributes |= MethodAttributes.Static;

            parameters.AddRange(methodInfo.GetParameters().Select(w => w.ParameterType));
            
            MethodBuilder builder = _interceptorTypeBuilder.DefineMethod(methodName, methodAttributes, 
                CallingConventions.Standard, methodInfo.ReturnType, parameters.ToArray());
            
            ILGenerator generator = builder.GetILGenerator();
            BuildBody(generator, parameters, methodInfo.ReturnType);

            TypeInfo type = _interceptorTypeBuilder.CreateTypeInfo();
            return type.GetMethod(methodName)!;
        }

        /// <summary>
        /// Create the body of method interceptor.
        /// </summary>
        /// <param name="generator">Generator of method.</param>
        /// <param name="parameters">Parameters of method.</param>
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
        private void BuildBody(ILGenerator generator, IEnumerable<Type> parameters, Type returnType)
        {
            bool isAwaitable = Method.IsAwaitable();
            int totalArgs = parameters.Count();

            if (Method.IsConstructor && !Method.IsStatic)
            {
                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Call, ObjectCtor);
            }

            generator.DeclareLocal(typeof(object[]));
            generator.Emit(OpCodes.Ldc_I4, totalArgs);
            generator.Emit(OpCodes.Newarr, typeof(object));
            generator.Emit(OpCodes.Stloc_0);

            if (totalArgs > 0)
            {
                int argIndex = 0;

                foreach (Type parameterType in parameters)
                {
                    generator.Emit(OpCodes.Ldloc_0);
                    generator.Emit(OpCodes.Ldc_I4, argIndex);

                    Type type = parameterType;

                    if (type.IsByRef)
                    {
                        generator.Emit(OpCodes.Ldarg_S, argIndex);
                        type = type.GetElementType()!;
                    }
                    else
                    {
                        generator.Emit(OpCodes.Ldarga_S, argIndex);
                    }

                    generator.Emit(OpCodes.Mkrefany, type);
                    generator.Emit(OpCodes.Call, GetReferenceFromTypedReference);
                    generator.Emit(OpCodes.Box, typeof(IntPtr));
                    generator.Emit(OpCodes.Stelem_Ref);

                    argIndex++;
                }
            }

            Type returnTypeInterceptor;

            if (returnType.IsPointer || returnType.IsByRef || returnType == typeof(void))
                returnTypeInterceptor = typeof(IntPtr);
            else if (isAwaitable && returnType.IsGenericType)
                returnTypeInterceptor = returnType.GetGenericArguments().First();
            else
                returnTypeInterceptor = returnType;

            MethodInfo getAwaiter = typeof(Task<>).MakeGenericType(returnTypeInterceptor).GetMethod(nameof(Task.GetAwaiter), BindingFlags.Public | BindingFlags.Instance)!;
            MethodInfo getResult = typeof(TaskAwaiter<>).MakeGenericType(returnTypeInterceptor).GetMethod(nameof(TaskAwaiter.GetResult), BindingFlags.Public | BindingFlags.Instance)!;
            LocalBuilder awaiterVariable = generator.DeclareLocal(typeof(TaskAwaiter<>).MakeGenericType(returnTypeInterceptor));

            generator.Emit(OpCodes.Ldc_I8, Method.MethodHandle.Value.ToInt64());
            generator.Emit(OpCodes.Newobj, ConstructorIntPtrLong);

            generator.Emit(OpCodes.Ldloca_S, 0);

            if (Method.IsGenericMethod || Method.DeclaringType!.IsGenericType)
                generator.Emit(OpCodes.Ldc_I4_1);
            else
                generator.Emit(OpCodes.Ldc_I4_0);

            generator.Emit(OpCodes.Newobj, ConstructorCallManager);
            generator.Emit(OpCodes.Dup);

            if (isAwaitable && returnType.IsGenericType || returnType.CanBeInline())
            {
                MethodInfo interceptor = InterceptAsyncCallAsync.MakeGenericMethod(returnTypeInterceptor);
                generator.Emit(OpCodes.Call, interceptor);
            }
            else
            {
                generator.Emit(OpCodes.Call, InterceptCallAsync);
            }

            generator.Emit(OpCodes.Call, getAwaiter);
            generator.Emit(OpCodes.Stloc, awaiterVariable.LocalIndex);
            generator.Emit(OpCodes.Ldloca_S, awaiterVariable.LocalIndex);
            generator.Emit(OpCodes.Call, getResult);

            if (HasReturn)
            {
                LocalBuilder retVariable;

                if (isAwaitable)
                {
                    retVariable = generator.DeclareLocal(returnTypeInterceptor);

                    generator.Emit(OpCodes.Stloc, retVariable.LocalIndex);
                    generator.Emit(OpCodes.Call, CallDispose);
                    generator.Emit(OpCodes.Ldloc, retVariable.LocalIndex);

                    if (returnType.IsTask())
                    {
                        MethodInfo fromResult = typeof(Task).GetMethod(nameof(Task.FromResult))!.MakeGenericMethod(returnTypeInterceptor);
                        generator.Emit(OpCodes.Call, fromResult);
                    }
                    else
                    {
                        ConstructorInfo ctorValueTask = returnType.GetConstructor(new[] {returnTypeInterceptor})!;
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
    }
}