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

        public FieldInfo ResolveField(int token, out bool isResolved)
        {
            _tokenResolver.Invoke(token, out IntPtr typeHandle, out _, out IntPtr fieldHandle);
            FieldInfo fieldInfo = ResolveField(fieldHandle, typeHandle);
            isResolved = true;
            return fieldInfo;
        }

        public MemberInfo ResolveMember(int token, out bool isResolved)
        {
            _tokenResolver.Invoke(token, out IntPtr typeHandle, out IntPtr methodHandle, out IntPtr fieldHandle);

            if (methodHandle != IntPtr.Zero)
            {
                MemberInfo memberInfo = ResolveMethod(methodHandle, typeHandle);
                isResolved = true;
                return memberInfo;
            }

            if (fieldHandle != IntPtr.Zero)
            {
                MemberInfo memberInfo = ResolveField(fieldHandle, typeHandle);
                isResolved = true;
                return memberInfo;
            }

            if (typeHandle != IntPtr.Zero)
            {
                MemberInfo memberInfo = _getTypeFromHandleUnsafe(typeHandle);
                isResolved = true;
                return memberInfo;
            }

            throw new NotSupportedException();
        }

        private MethodBase ResolveMethod(IntPtr methodHandle, IntPtr typeHandle)
        {
            return (MethodBase) _getMethodBase.Invoke(null, new[]
            {
                typeHandle == IntPtr.Zero ? null : _getTypeFromHandleUnsafe(typeHandle),
                _runtimeMethodHandleInternalCtor.Invoke(new object[] {methodHandle})
            });
        }

        public MethodBase ResolveMethod(int token, out bool isResolved)
        {
            _tokenResolver.Invoke(token, out IntPtr typeHandle, out IntPtr methodHandle, out _);
            MethodBase methodBase = ResolveMethod(methodHandle, typeHandle);
            isResolved = true;
            return methodBase;
        }

        public byte[] ResolveSignature(int token, out bool isResolved)
        {
            byte[] signature = _signatureResolver.Invoke(token, 0);
            isResolved = true;
            return signature;
        }

        public string ResolveString(int token, out bool isResolved)
        {
            string @string = _stringResolver.Invoke(token);
            isResolved = true;
            return @string;
        }

        public Type ResolveType(int token, out bool isResolved)
        {
            _tokenResolver.Invoke(token, out IntPtr typeHandle, out _, out _);
            Type type = _getTypeFromHandleUnsafe(typeHandle);
            isResolved = true;
            return type;
        }

        public Type ResolveType(int token, Type[]? genericTypeArguments, Type[]? genericMethodArguments, out bool isResolved)
        {
            if (genericTypeArguments is {Length: > 0})
                throw new NotImplementedException();

            if (genericMethodArguments is {Length: > 0})
                throw new NotImplementedException();

            return ResolveType(token, out isResolved);
        }

        public MethodBase ResolveMethod(int token, Type[]? genericTypeArguments, Type[]? genericMethodArguments, out bool isResolved)
        {
            if (genericTypeArguments is {Length: > 0})
                throw new NotImplementedException();

            if (genericMethodArguments is {Length: > 0})
                throw new NotImplementedException();

            return ResolveMethod(token, out isResolved);
        }

        public MemberInfo ResolveMember(int token, Type[]? genericTypeArguments, Type[]? genericMethodArguments, out bool isResolved)
        {
            if (genericTypeArguments is {Length: > 0})
                throw new NotImplementedException();

            if (genericMethodArguments is {Length: > 0})
                throw new NotImplementedException();

            return ResolveMember(token, out isResolved);
        }

        public FieldInfo ResolveField(int token, Type[]? genericTypeArguments, Type[]? genericMethodArguments, out bool isResolved)
        {
            if (genericTypeArguments is {Length: > 0})
                throw new NotImplementedException();

            if (genericMethodArguments is {Length: > 0})
                throw new NotImplementedException();

            return ResolveField(token, out isResolved);
        }
    }
}