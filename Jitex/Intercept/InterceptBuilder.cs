using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Jitex.Intercept
{
    /// <summary>
    /// Prepare a method to intercept call.
    /// </summary>
    internal class InterceptBuilder
    {
        /// <summary>
        /// Method original.
        /// </summary>
        public MethodBase Method { get; }

        private static readonly MethodInfo InterceptCall;
        private static readonly MethodInfo CallDispose;
        private static readonly ConstructorInfo ObjectCtor;
        private static readonly ConstructorInfo ConstructorCallManager;
        private static readonly ConstructorInfo ConstructorIntPtrLong;

        static InterceptBuilder()
        {
            ObjectCtor = typeof(object).GetConstructor(Type.EmptyTypes)!;
            ConstructorCallManager = typeof(CallManager).GetConstructor(new[] { typeof(IntPtr), typeof(object[]).MakeByRefType(), typeof(bool) })!;
            InterceptCall = typeof(CallManager).GetMethod(nameof(CallManager.InterceptCall), BindingFlags.Public | BindingFlags.Instance)!;
            ConstructorIntPtrLong = typeof(IntPtr).GetConstructor(new[] { typeof(long) })!;
            CallDispose = typeof(CallManager).GetMethod(nameof(CallManager.Dispose), BindingFlags.Public | BindingFlags.Instance)!;
        }

        /// <summary>
        /// Create a builder
        /// </summary>
        /// <param name="method">Method original which will be intercept.</param>
        public InterceptBuilder(MethodBase method)
        {
            Method = method;
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
            MethodInfo methodInfo = (MethodInfo)Method;

            List<Type> parameters = new List<Type>();

            if (Method.IsGenericMethod)
                parameters.Add(typeof(IntPtr));

            if (!Method.IsStatic)
                parameters.Add(typeof(IntPtr));

            parameters.AddRange(methodInfo.GetParameters().Select(w => w.ParameterType));

            DynamicMethod methodIntercept = new(Method.Name + "Jitex", MethodAttributes.Static | MethodAttributes.Public, CallingConventions.Standard, methodInfo.ReturnType, parameters.ToArray(), methodInfo.DeclaringType, true);

            ILGenerator generator = methodIntercept.GetILGenerator();

            BuildBody(generator, parameters, methodInfo.ReturnType);

            return methodIntercept;
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
        ///    ...
        ///
        ///    Type[] genericMethodArgs...
        ///    Type[] genericTypeArgs...
        ///
        ///    return InterceptCall(handle,args,genericTypeArgs,genericMethodArgs);
        /// }
        /// </remarks>
        private void BuildBody(ILGenerator generator, IEnumerable<Type> parameters, Type returnType)
        {
            int totalArgs = parameters.Count();

            LocalBuilder lastLocalVariable;

            if (Method.IsConstructor && !Method.IsStatic)
            {
                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Call, ObjectCtor);
            }

            if (totalArgs > 0)
            {
                lastLocalVariable = generator.DeclareLocal(typeof(object[]));

                bool typerefDeclared = false;

                generator.Emit(OpCodes.Ldc_I4, totalArgs);
                generator.Emit(OpCodes.Newarr, typeof(object));
                generator.Emit(OpCodes.Stloc_0);

                int argIndex = 0;

                foreach (Type parameterType in parameters)
                {
                    Type type = parameterType;

                    if (!typerefDeclared)
                    {
                        lastLocalVariable = generator.DeclareLocal(typeof(TypedReference));
                        typerefDeclared = true;
                    }

                    if (type.IsByRef)
                    {
                        generator.Emit(OpCodes.Ldarg_S, argIndex);

                        type = type.GetElementType()!;
                        Debug.Assert(type != null);
                    }
                    else
                    {
                        generator.Emit(OpCodes.Ldarga_S, argIndex);
                    }

                    generator.Emit(OpCodes.Mkrefany, type);
                    generator.Emit(OpCodes.Stloc_1);
                    generator.Emit(OpCodes.Ldloc_0);
                    generator.Emit(OpCodes.Ldc_I4, argIndex);
                    generator.Emit(OpCodes.Ldloca_S, 1);
                    generator.Emit(OpCodes.Conv_U);
                    generator.Emit(OpCodes.Ldind_I);
                    generator.Emit(OpCodes.Box, typeof(IntPtr));
                    generator.Emit(OpCodes.Stelem_Ref);

                    argIndex++;
                }
            }
            else
            {
                generator.Emit(OpCodes.Ldnull);
            }

            lastLocalVariable = generator.DeclareLocal(typeof(IntPtr));

            generator.Emit(OpCodes.Ldc_I8, Method.MethodHandle.Value.ToInt64());
            generator.Emit(OpCodes.Newobj, ConstructorIntPtrLong);

            generator.Emit(OpCodes.Ldloca_S, 0);

            if (Method.IsGenericMethod || Method.DeclaringType!.IsGenericType)
                generator.Emit(OpCodes.Ldc_I4_1);
            else
                generator.Emit(OpCodes.Ldc_I4_0);

            generator.Emit(OpCodes.Newobj, ConstructorCallManager);
            generator.Emit(OpCodes.Dup);
            generator.Emit(OpCodes.Call, InterceptCall);

            if (returnType == typeof(void))
            {
                generator.Emit(OpCodes.Pop);
                generator.Emit(OpCodes.Call, CallDispose);
            }
            else
            {
                generator.Emit(OpCodes.Stloc_S, lastLocalVariable.LocalIndex);
                generator.Emit(OpCodes.Call, CallDispose);
                generator.Emit(OpCodes.Ldloc_S, lastLocalVariable.LocalIndex);
            }

            if (returnType != typeof(void) && returnType.IsValueType)
                generator.Emit(OpCodes.Unbox_Any, returnType);

            generator.Emit(OpCodes.Ret);
        }
    }
}