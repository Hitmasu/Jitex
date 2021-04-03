using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using IntPtr = System.IntPtr;

namespace Jitex.Utils
{
    /// <summary>
    /// Helpers to manage delegate
    /// </summary>
    public static class DelegateHelper
    {
        private static readonly MethodInfo MakeNewCustomDelegate;

        static DelegateHelper()
        {
            MakeNewCustomDelegate = typeof(Expression).Assembly.GetType("System.Linq.Expressions.Compiler.DelegateHelpers").GetMethod("MakeNewCustomDelegate", BindingFlags.NonPublic | BindingFlags.Static);
        }

        public static IList<Type> CreateParameters(MethodBase method)
        {
            IList<Type> parameters = new List<Type>();

            if (!method.IsStatic)
                parameters.Add(typeof(IntPtr));

            if (method.IsGenericMethod)
                parameters.Add(typeof(IntPtr));

            foreach (ParameterInfo parameter in method.GetParameters())
            {
                if (parameter.ParameterType.IsPrimitive)
                    parameters.Add(parameter.ParameterType);
                else
                    parameters.Add(typeof(IntPtr));
            }

            return parameters;
        }

        public static Delegate BuildDelegate(IntPtr addressMethod, MethodBase method)
        {
            IList<Type> parameters = CreateParameters(method);
            Type[] parametersArray = parameters.ToArray();

            Type retType;
            Type boxType = default;
            if (method is ConstructorInfo)
            {
                retType = typeof(void);
            }
            else
            {
                MethodInfo methodInfo = (MethodInfo) method;

                if (methodInfo.ReturnType == typeof(void))
                {
                    retType = typeof(void);
                }
                else if (!methodInfo.ReturnType.IsPrimitive)
                {
                    boxType = typeof(IntPtr);
                    retType = typeof(object);
                }
                else
                {
                    boxType = methodInfo.ReturnType;
                    retType = typeof(object);
                }
            }

            DynamicMethod dm = new($"{method.Name}Original", retType, parametersArray, method.DeclaringType, true);
            ILGenerator generator = dm.GetILGenerator();

            for (int i = 0; i < parameters.Count; i++)
                generator.Emit(OpCodes.Ldarg, i);

            generator.Emit(OpCodes.Ldc_I8, addressMethod.ToInt64());
            generator.Emit(OpCodes.Conv_I);
            generator.EmitCalli(OpCodes.Calli, CallingConventions.Standard, retType, parametersArray, null);

            if (retType != typeof(void))
                generator.Emit(OpCodes.Box, boxType);
                
            generator.Emit(OpCodes.Ret);

            Type delegateType;
            
            if (retType == typeof(void))
            {
                delegateType = Expression.GetActionType(parameters.ToArray());
            }
            else
            {
                parameters.Add(retType);
                delegateType = Expression.GetFuncType(parameters.ToArray());
            }

            return dm.CreateDelegate(delegateType);
        }

        public static Delegate CreateDelegate(IntPtr address, MethodBase method)
        {
            return BuildDelegate(address, method);
            //Type delegateType = CreateTypeDelegate(method);
            //return Marshal.GetDelegateForFunctionPointer(address, delegateType);
        }
    }
}