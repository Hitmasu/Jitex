using System;
using System.Reflection;
using System.Reflection.Emit;

namespace Jitex.Builder.IL.Resolver
{
    internal sealed class DynamicMethodTokenResolver : ITokenResolver
    {
        private delegate Type GetTypeFromHandleUnsafe(IntPtr handle);

        private delegate byte[] SignatureResolver(int token, int fromMethod);

        private delegate string StringResolver(int token);

        private delegate void TokenResolver(int token, out IntPtr typeHandle, out IntPtr methodHandle, out IntPtr fieldHandle);

        private readonly MethodInfo _getFieldInfo;
        private readonly MethodInfo _getMethodBase;
        private readonly GetTypeFromHandleUnsafe _getTypeFromHandleUnsafe;
        private readonly ConstructorInfo _runtimeFieldHandleStubCtor;
        private readonly ConstructorInfo _runtimeMethodHandleInternalCtor;
        private readonly SignatureResolver _signatureResolver;
        private readonly StringResolver _stringResolver;

        private readonly TokenResolver _tokenResolver;

        public DynamicMethodTokenResolver(DynamicMethod dynamicMethod)
        {
            object resolver = typeof(DynamicMethod).GetField("m_resolver", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(dynamicMethod);

            _tokenResolver = (TokenResolver) resolver.GetType().GetMethod("ResolveToken", BindingFlags.Instance | BindingFlags.NonPublic).CreateDelegate(typeof(TokenResolver), resolver);
            _stringResolver = (StringResolver) resolver.GetType().GetMethod("GetStringLiteral", BindingFlags.Instance | BindingFlags.NonPublic).CreateDelegate(typeof(StringResolver), resolver);
            _signatureResolver = (SignatureResolver) resolver.GetType().GetMethod("ResolveSignature", BindingFlags.Instance | BindingFlags.NonPublic).CreateDelegate(typeof(SignatureResolver), resolver);

            _getTypeFromHandleUnsafe = (GetTypeFromHandleUnsafe) typeof(Type).GetMethod("GetTypeFromHandleUnsafe", BindingFlags.Static | BindingFlags.NonPublic, null, new[] {typeof(IntPtr)}, null).CreateDelegate(typeof(GetTypeFromHandleUnsafe), null);
            Type runtimeType = typeof(RuntimeTypeHandle).Assembly.GetType("System.RuntimeType");

            Type runtimeMethodHandleInternal = typeof(RuntimeTypeHandle).Assembly.GetType("System.RuntimeMethodHandleInternal");
            _getMethodBase = runtimeType.GetMethod("GetMethodBase", BindingFlags.Static | BindingFlags.NonPublic, null, new[] {runtimeType, runtimeMethodHandleInternal}, null);
            _runtimeMethodHandleInternalCtor = runtimeMethodHandleInternal.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, new[] {typeof(IntPtr)}, null);

            Type runtimeFieldInfoStub = typeof(RuntimeTypeHandle).Assembly.GetType("System.RuntimeFieldInfoStub");
            _runtimeFieldHandleStubCtor = runtimeFieldInfoStub.GetConstructor(BindingFlags.Instance | BindingFlags.Public, null, new[] {typeof(IntPtr), typeof(object)}, null);
            _getFieldInfo = runtimeType.GetMethod("GetFieldInfo", BindingFlags.Static | BindingFlags.NonPublic, null, new[] {runtimeType, typeof(RuntimeTypeHandle).Assembly.GetType("System.IRuntimeFieldInfo")}, null);
        }

        private FieldInfo ResolveField(IntPtr fieldHandle, IntPtr typeHandle)
        {
            return (FieldInfo) _getFieldInfo.Invoke(null, new[]
            {
                typeHandle == IntPtr.Zero ? null : _getTypeFromHandleUnsafe(typeHandle),
                _runtimeFieldHandleStubCtor.Invoke(new object[] {fieldHandle, null})
            });
        }

        public FieldInfo ResolveField(int token)
        {
            _tokenResolver.Invoke(token, out IntPtr typeHandle, out _, out IntPtr fieldHandle);
            return ResolveField(fieldHandle, typeHandle);
        }

        public FieldInfo ResolveField(int token, Type[] genericTypeArguments, Type[] genericMethodArguments)
        {
            throw new NotImplementedException();
        }

        public MemberInfo ResolveMember(int token)
        {
            _tokenResolver.Invoke(token, out IntPtr typeHandle, out IntPtr methodHandle, out IntPtr fieldHandle);

            if (methodHandle != IntPtr.Zero)
            {
                return ResolveMethod(methodHandle, typeHandle);
            }

            if (fieldHandle != IntPtr.Zero)
            {
                return ResolveField(fieldHandle, typeHandle);
            }

            if (typeHandle != IntPtr.Zero)
            {
                return _getTypeFromHandleUnsafe(typeHandle);
            }

            throw new NotSupportedException();
        }

        public MemberInfo ResolveMember(int token, Type[] genericTypeArguments, Type[] genericMethodArguments)
        {
            throw new NotImplementedException();
        }

        private MethodBase ResolveMethod(IntPtr methodHandle, IntPtr typeHandle)
        {
            return (MethodBase) _getMethodBase.Invoke(null, new[]
            {
                typeHandle == IntPtr.Zero ? null : _getTypeFromHandleUnsafe(typeHandle),
                _runtimeMethodHandleInternalCtor.Invoke(new object[] {methodHandle})
            });
        }
        
        public MethodBase ResolveMethod(int token)
        {
            _tokenResolver.Invoke(token, out IntPtr typeHandle, out IntPtr methodHandle, out _);
            return ResolveMethod(methodHandle, typeHandle);
        }

        public MethodBase ResolveMethod(int token, Type[] genericTypeArguments, Type[] genericMethodArguments)
        {
            throw new NotImplementedException();
        }

        public byte[] ResolveSignature(int token)
        {
            return _signatureResolver.Invoke(token, 0);
        }

        public string ResolveString(int token)
        {
            return _stringResolver.Invoke(token);
        }

        public Type ResolveType(int token)
        {
            _tokenResolver.Invoke(token, out IntPtr typeHandle, out _, out _);
            return _getTypeFromHandleUnsafe(typeHandle);
        }

        public Type ResolveType(int token, Type[] genericTypeArguments, Type[] genericMethodArguments)
        {
            throw new NotImplementedException();
        }
    }
}