using System;
using System.Reflection;
using System.Reflection.Emit;

namespace Jitex.Internal
{
    internal class TokenGenerator
    {
        private static readonly MethodInfo GetMemberRefToken;
        public DynamicMethod Method { get; }

        public IntPtr Context => GetDynamicMethodRuntimeHandle(Method).Value;

        static TokenGenerator()
        {
            Type? dynamicILGenerator = Type.GetType("System.Reflection.Emit.DynamicILGenerator");
            GetMemberRefToken = dynamicILGenerator.GetMethod("GetMemberRefToken", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        public TokenGenerator(MethodInfo method)
        {
            Method = new DynamicMethod(method.Name + "Generated", typeof(void), Type.EmptyTypes, method.Module);
        }

        public int GenerateToken(MethodInfo method)
        {
            var l = method.DeclaringType.GetMethod("Hook").MakeGenericMethod(typeof(int));
            ILGenerator generator = Method.GetILGenerator();
            generator.EmitCall(OpCodes.Call, l, null);
            
            generator.Emit(OpCodes.Pop);
            generator.Emit(OpCodes.Ret);

            return (int)GetMemberRefToken.Invoke(generator, new[] { method, (object)null });
        }

        private static RuntimeMethodHandle GetDynamicMethodRuntimeHandle(MethodBase method) {
            RuntimeMethodHandle handle;    
                var getMethodDescriptorInfo = typeof(DynamicMethod).GetMethod("GetMethodDescriptor", BindingFlags.NonPublic | BindingFlags.Instance);
                handle = (RuntimeMethodHandle) getMethodDescriptorInfo.Invoke(method, null);
            return handle;
        }
    }
}
