using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Jitex.Utils;
using Jitex.Utils.Extension;

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

        public bool HasReturn { get; private set; }

        private static readonly MethodInfo InterceptCallAsync;
        private static readonly MethodInfo GetAwaiterGeneric;
        private static readonly MethodInfo GetResultGeneric;
        private static readonly MethodInfo CallDispose;
        private static readonly MethodInfo GetReferenceFromTypedReference;
        private static readonly ConstructorInfo ObjectCtor;
        private static readonly ConstructorInfo ConstructorCallManager;
        private static readonly ConstructorInfo ConstructorIntPtrLong;

        static InterceptBuilder()
        {
            ObjectCtor = typeof(object).GetConstructor(Type.EmptyTypes)!;
            GetAwaiterGeneric = typeof(Task<object>).GetMethod(nameof(Task<object>.GetAwaiter), BindingFlags.Public | BindingFlags.Instance)!;
            GetResultGeneric = typeof(TaskAwaiter<object>).GetMethod(nameof(TaskAwaiter<object>.GetResult), BindingFlags.Public | BindingFlags.Instance)!;
            ConstructorCallManager = typeof(CallManager).GetConstructor(new[] { typeof(IntPtr), typeof(object[]).MakeByRefType(), typeof(bool) })!;
            InterceptCallAsync = typeof(CallManager).GetMethod(nameof(CallManager.InterceptCallAsync), BindingFlags.Public | BindingFlags.Instance)!;
            ConstructorIntPtrLong = typeof(IntPtr).GetConstructor(new[] { typeof(long) })!;
            CallDispose = typeof(CallManager).GetMethod(nameof(CallManager.Dispose), BindingFlags.Public | BindingFlags.Instance)!;
            GetReferenceFromTypedReference = typeof(TypeHelper).GetMethod(nameof(TypeHelper.GetReferenceFromTypedReference))!;
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

            HasReturn = methodInfo.ReturnType == typeof(void) || methodInfo.ReturnType == typeof(Task) || methodInfo.ReturnType == typeof(ValueTask);

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
        ///    ....
        ///    return InterceptCall();
        /// }
        /// </remarks>
        private void BuildBody(ILGenerator generator, IEnumerable<Type> parameters, Type returnType)
        {
            int totalArgs = parameters.Count();

            if (Method.IsConstructor && !Method.IsStatic)
            {
                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Call, ObjectCtor);
            }

            generator.DeclareLocal(typeof(object[]));

            if (totalArgs > 0)
            {
                generator.Emit(OpCodes.Ldc_I4, totalArgs);
                generator.Emit(OpCodes.Newarr, typeof(object));
                generator.Emit(OpCodes.Stloc_0);

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
            else
            {
                generator.Emit(OpCodes.Ldnull);
                generator.Emit(OpCodes.Stloc_0);
            }

            LocalBuilder awaiterVariable = generator.DeclareLocal(GetRetType(returnType));

            generator.Emit(OpCodes.Ldc_I8, Method.MethodHandle.Value.ToInt64());
            generator.Emit(OpCodes.Newobj, ConstructorIntPtrLong);

            generator.Emit(OpCodes.Ldloca_S, 0);

            if (Method.IsGenericMethod || Method.DeclaringType!.IsGenericType)
                generator.Emit(OpCodes.Ldc_I4_1);
            else
                generator.Emit(OpCodes.Ldc_I4_0);

            generator.Emit(OpCodes.Newobj, ConstructorCallManager);
            generator.Emit(OpCodes.Dup);

            //if (Method.IsAwaitable())
            //{

            generator.Emit(OpCodes.Call, InterceptCallAsync);

            generator.Emit(OpCodes.Call, GetAwaiterGeneric);
            generator.Emit(OpCodes.Stloc_S, awaiterVariable.LocalIndex);
            generator.Emit(OpCodes.Ldloca_S, awaiterVariable.LocalIndex);

            generator.Emit(OpCodes.Call, GetResultGeneric);

            if (HasReturn)
            {
                LocalBuilder retVariable = generator.DeclareLocal(typeof(IntPtr));
                generator.Emit(OpCodes.Stloc_S, retVariable.LocalIndex);
                generator.Emit(OpCodes.Call, CallDispose);
                generator.Emit(OpCodes.Ldloc_S, retVariable.LocalIndex);
            }
            else
            {
                generator.Emit(OpCodes.Pop);
                generator.Emit(OpCodes.Call, CallDispose);
            }

            //if (HasReturn)
            //{
            //    Type retTaskType = returnType.GetGenericArguments().First();
            //    MethodInfo fromResult = typeof(Task).GetMethod(nameof(Task.FromResult))!.MakeGenericMethod(retTaskType);
            //    generator.Emit(OpCodes.Call, fromResult);
            //}

            //}
            //else
            //{
            //    generator.Emit(OpCodes.Call, InterceptCall);

            //    if (returnType == typeof(void))
            //    {
            //        generator.Emit(OpCodes.Pop);
            //        generator.Emit(OpCodes.Call, CallDispose);
            //    }
            //    else
            //    {
            //        generator.Emit(OpCodes.Stloc_S, retVariable.LocalIndex);
            //        generator.Emit(OpCodes.Call, CallDispose);
            //        generator.Emit(OpCodes.Ldloc_S, retVariable.LocalIndex);
            //    }

            //    if (returnType != typeof(void) && returnType.IsValueType)
            //        generator.Emit(OpCodes.Unbox_Any, returnType);
            //}

            generator.Emit(OpCodes.Ret);
        }

        public static Type GetRetType(Type type)
        {
            if (type == typeof(Task) || type == typeof(ValueTask))
                return typeof(Task);

            if (type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(Task<>) || type.GetGenericTypeDefinition() == typeof(ValueTask)))
                return type.GetGenericArguments().First();

            return type;
        }
    }
}