using Jitex.Hook;
using Jitex.JIT.CorInfo;
using Jitex.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Jitex.Builder.IL;
using Jitex.Exceptions;
using Jitex.JIT.Context;
using Jitex.Runtime;
using Jitex.Utils.Extension;
using static Jitex.JIT.JitexHandler;
using Exception = System.Exception;
using MethodBody = Jitex.Builder.Method.MethodBody;
using MethodInfo = Jitex.JIT.CorInfo.MethodInfo;

namespace Jitex.JIT
{
    /// <summary>
    /// Handlers to expose hooks.
    /// </summary>
    public static class JitexHandler
    {
        /// <summary>
        /// Method resolver handler.
        /// </summary>
        /// <param name="context">Context of method.</param>
        public delegate void MethodResolverHandler(MethodContext context);

        /// <summary>
        /// Token resolver handler.
        /// </summary>
        /// <param name="context">Context of token.</param>
        public delegate void TokenResolverHandler(TokenContext context);
    }

    /// <summary>
    /// Hook instance from JIT.
    /// </summary>
    internal sealed class ManagedJit : IDisposable
    {
        /// <summary>
        /// Lock to prevent multiple instance.
        /// </summary>
        private static readonly object InstanceLock = new object();

        /// <summary>
        /// Lock to prevent unload in compile time.
        /// </summary>
        private static readonly object JitLock = new object();

        /// <summary>
        /// Current instance of JIT.
        /// </summary>
        private static ManagedJit? _instance;

        [ThreadStatic] private static CompileTls? _compileTls;

        [ThreadStatic] private static TokenTls? _tokenTls;

        private readonly HookManager _hookManager = new HookManager();

        /// <summary>
        /// Running framework.
        /// </summary>
        private readonly RuntimeFramework _framework;

        /// <summary>
        /// Custom compíle method.
        /// </summary>
        private RuntimeFramework.CompileMethodDelegate _compileMethod;

        /// <summary>
        /// Custom resolve token.
        /// </summary>
        private CEEInfo.ResolveTokenDelegate _resolveToken;

        /// <summary>
        /// Custom construct string literal.
        /// </summary>
        private CEEInfo.ConstructStringLiteralDelegate _constructStringLiteral;

        private bool _isDisposed;

        private MethodResolverHandler? _methodResolvers;

        private TokenResolverHandler? _tokenResolvers;

        public bool IsLoaded => _instance != null;

        public bool IsEnabled { get; private set; }

        /// <summary>
        ///     Prepare custom JIT.
        /// </summary>
        private ManagedJit()
        {
            _framework = RuntimeFramework.GetFramework();

            _compileMethod = CompileMethod;
            _resolveToken = ResolveToken;
            _constructStringLiteral = ConstructStringLiteral;

            RuntimeHelperExtension.PrepareDelegate(_compileMethod, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, (uint)0, IntPtr.Zero, 0);
            RuntimeHelperExtension.PrepareDelegate(_resolveToken, IntPtr.Zero, IntPtr.Zero);
            RuntimeHelperExtension.PrepareDelegate(_constructStringLiteral, IntPtr.Zero, IntPtr.Zero, 0, IntPtr.Zero);

            _hookManager.InjectHook(_framework.ICorJitCompileVTable, _compileMethod);
            IsEnabled = true;
        }

        /// <summary>
        /// Get singleton instance from ManagedJit.
        /// </summary>
        /// <returns></returns>
        internal static ManagedJit GetInstance()
        {
            lock (InstanceLock)
            {
                return _instance ??= new ManagedJit();
            }
        }

        internal void AddMethodResolver(MethodResolverHandler methodResolver) => _methodResolvers += methodResolver;

        internal void AddTokenResolver(TokenResolverHandler tokenResolver) => _tokenResolvers += tokenResolver;

        internal void RemoveMethodResolver(MethodResolverHandler methodResolver) => _methodResolvers -= methodResolver;

        internal void RemoveTokenResolver(TokenResolverHandler tokenResolver) => _tokenResolvers -= tokenResolver;

        internal bool HasMethodResolver(MethodResolverHandler methodResolver) => _methodResolvers != null && _methodResolvers.GetInvocationList().Any(del => del.Method == methodResolver.Method);

        internal bool HasTokenResolver(TokenResolverHandler tokenResolver) => _tokenResolvers != null && _tokenResolvers.GetInvocationList().Any(del => del.Method == tokenResolver.Method);

        /// <summary>
        /// Enable Jitex hooks
        /// </summary>
        internal void Enable()
        {
            lock (JitLock)
            {
                _hookManager.InjectHook(_framework.ICorJitCompileVTable, _compileMethod);
                _hookManager.InjectHook(CEEInfo.ResolveTokenIndex, _resolveToken);
                _hookManager.InjectHook(CEEInfo.ConstructStringLiteralIndex, _constructStringLiteral);
            }

            IsEnabled = true;
        }

        /// <summary>
        /// Disable Jitex hooks
        /// </summary>
        internal void Disable()
        {
            lock (JitLock)
            {
                _hookManager.RemoveHook(_resolveToken);
                _hookManager.RemoveHook(_compileMethod);
                _hookManager.RemoveHook(_constructStringLiteral);
            }

            IsEnabled = false;
        }

        /// <summary>
        ///     Wrap delegate to compileMethod from ICorJitCompiler.
        /// </summary>
        /// <param name="thisPtr">this parameter (pointer to CILJIT).</param>
        /// <param name="comp">(IN) - Pointer to ICorJitInfo.</param>
        /// <param name="info">(IN) - Pointer to CORINFO_METHOD_INFO.</param>
        /// <param name="flags">(IN) - Pointer to CorJitFlag.</param>
        /// <param name="nativeEntry">(OUT) - Pointer to NativeEntry.</param>
        /// <param name="nativeSizeOfCode">(OUT) - Size of NativeEntry.</param>
        private CorJitResult CompileMethod(IntPtr thisPtr, IntPtr comp, IntPtr info, uint flags, out IntPtr nativeEntry, out int nativeSizeOfCode)
        {
            if (thisPtr == default)
            {
                nativeEntry = IntPtr.Zero;
                nativeSizeOfCode = 0;
                return 0;
            }

            _compileTls ??= new CompileTls();
            _compileTls.EnterCount++;

            try
            {
                MethodContext? methodContext = null;
                IntPtr sigAddress = IntPtr.Zero;
                IntPtr ilAddress = IntPtr.Zero;

                if (_compileTls.EnterCount == 1 && _methodResolvers != null)
                {
                    IEnumerable<Delegate> resolvers = _methodResolvers.GetInvocationList();

                    if (resolvers.Any())
                    {
                        MethodInfo methodInfo = new MethodInfo(info);

                        lock (JitLock)
                        {
                            if (_framework.CEEInfoVTable == IntPtr.Zero)
                            {
                                _framework.ReadICorJitInfoVTable(comp);

                                _hookManager.InjectHook(CEEInfo.ResolveTokenIndex, _resolveToken);
                                _hookManager.InjectHook(CEEInfo.ConstructStringLiteralIndex, _constructStringLiteral);
                            }
                        }

                        MethodBase methodFound = MethodHelper.GetMethodFromHandle(methodInfo.MethodHandle)!;

                        if (methodFound != null)
                        {
                            if (DynamicHelpers.IsDynamicScope(methodInfo.Scope))
                            {
                                methodFound = DynamicHelpers.GetOwner(methodFound);
                            }

                            MethodBase? source = _compileTls.GetSource();

                            methodContext = new MethodContext(methodFound, source);

                            foreach (MethodResolverHandler resolver in resolvers)
                            {
                                resolver(methodContext);

                                if (methodContext.IsResolved)
                                    break;
                            }

                            _tokenTls = new TokenTls();
                        }

                        if (methodContext != null && methodContext.IsResolved)
                        {
                            int ilLength = 0;

                            if (methodContext.Mode == MethodContext.ResolveMode.IL)
                            {
                                MethodBody methodBody = methodContext.Body;

                                ilLength = methodBody.IL.Length;

                                ilAddress = methodBody.IL.ToPointer();

                                if (methodBody.HasLocalVariable)
                                {
                                    byte[] signatureVariables = methodBody.GetSignatureVariables();
                                    sigAddress = signatureVariables.ToPointer();

                                    methodInfo.Locals.Signature = sigAddress + 1;
                                    methodInfo.Locals.Args = sigAddress + 3;
                                    methodInfo.Locals.NumArgs = (ushort)methodBody.LocalVariables.Count;
                                }

                                methodInfo.MaxStack = methodBody.MaxStackSize;
                            }
                            else
                            {
                                if (!methodContext.IsDetour)
                                {
                                    (ilAddress, ilLength) = PrepareIL(methodContext);

                                    if (methodInfo.MaxStack < 8)
                                        methodInfo.MaxStack = 8;
                                }
                            }

                            if (!methodContext.IsDetour)
                            {
                                methodInfo.EHCount = methodContext.Body.EHCount;
                                methodInfo.ILCode = ilAddress;
                                methodInfo.ILCodeSize = (uint)ilLength;
                            }
                        }
                    }
                }

                CorJitResult result = _framework.CompileMethod(thisPtr, comp, info, flags, out nativeEntry, out nativeSizeOfCode);

                if (ilAddress != IntPtr.Zero && methodContext!.Mode == MethodContext.ResolveMode.IL)
                    Marshal.FreeHGlobal(ilAddress);

                if (sigAddress != IntPtr.Zero)
                    Marshal.FreeHGlobal(sigAddress);

                if (methodContext?.Mode == MethodContext.ResolveMode.NATIVE)
                    Marshal.Copy(methodContext.NativeCode!, 0, nativeEntry, methodContext.NativeCode!.Length);

                return result;
            }
            finally
            {
                _compileTls.EnterCount--;
            }
        }

        private void ResolveToken(IntPtr thisHandle, IntPtr pResolvedToken)
        {
            _tokenTls ??= new TokenTls();
            _tokenTls.EnterCount++;

            if (thisHandle == IntPtr.Zero)
            {
                return;
            }

            try
            {
                if (_tokenTls.EnterCount == 1 && _tokenResolvers != null)
                {
                    IEnumerable<Delegate> resolvers = _tokenResolvers.GetInvocationList();

                    if (!resolvers.Any())
                    {
                        CEEInfo.ResolveToken(thisHandle, pResolvedToken);
                        return;
                    }

                    ResolvedToken resolvedToken = new ResolvedToken(pResolvedToken);

                    if (resolvedToken.Module != null)
                    {
                        MethodBase? source = MethodHelper.GetFromCache(resolvedToken.Context);

                        if (source == null)
                            source = _tokenTls.GetSource();

                        TokenContext context = new TokenContext(ref resolvedToken, source);

                        foreach (TokenResolverHandler resolver in resolvers)
                        {
                            resolver(context);
                        }
                    }
                }

                CEEInfo.ResolveToken(thisHandle, pResolvedToken);
            }
            finally
            {
                _tokenTls.EnterCount--;
            }
        }

        private InfoAccessType ConstructStringLiteral(IntPtr thisHandle, IntPtr hModule, int metadataToken, IntPtr ppValue)
        {
            if (thisHandle == IntPtr.Zero)
                return default;

            _tokenTls ??= new TokenTls();

            _tokenTls.EnterCount++;

            try
            {
                if (_tokenTls.EnterCount == 1 && _tokenResolvers != null)
                {
                    IEnumerable<Delegate> resolvers = _tokenResolvers.GetInvocationList();

                    if (!resolvers.Any())
                    {
                        return CEEInfo.ConstructStringLiteral(thisHandle, hModule, metadataToken, ppValue);
                    }

                    //Capture method who trying resolve that token.
                    MethodBase source = _tokenTls.GetSource();

                    ConstructString constructString = new ConstructString(hModule, metadataToken, ppValue);
                    TokenContext context = new TokenContext(constructString, source);

                    foreach (TokenResolverHandler resolver in resolvers)
                    {
                        resolver(context);

                        if (context.IsResolved)
                        {
                            if (string.IsNullOrEmpty(context.Content))
                                throw new StringNullOrEmptyException();

                            InfoAccessType result = CEEInfo.ConstructStringLiteral(thisHandle, hModule, metadataToken, ppValue);
                            WriteString(ppValue, context.Content!);
                            return result;
                        }
                    }
                }

                return CEEInfo.ConstructStringLiteral(thisHandle, hModule, metadataToken, ppValue);
            }
            finally
            {
                _tokenTls.EnterCount--;
            }
        }

        /// <summary>
        /// Write string on OBJECTHANDLE.
        /// </summary>
        /// <param name="ppValue">Pointer to OBJECTHANDLE.</param>
        /// <param name="content">Content to write.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void WriteString(IntPtr ppValue, string content)
        {
            IntPtr pEntry = Marshal.ReadIntPtr(ppValue);

            IntPtr objectHandle = Marshal.ReadIntPtr(pEntry);
            IntPtr hashMapPtr = Marshal.ReadIntPtr(objectHandle);

            byte[] newContent = Encoding.Unicode.GetBytes(content);

            objectHandle = Marshal.AllocHGlobal(IntPtr.Size + sizeof(int) + newContent.Length);

            Marshal.WriteIntPtr(objectHandle, hashMapPtr);
            Marshal.WriteInt32(objectHandle + IntPtr.Size, newContent.Length / 2);
            Marshal.Copy(newContent, 0, objectHandle + IntPtr.Size + sizeof(int), newContent.Length);

            Marshal.WriteIntPtr(pEntry, objectHandle);
        }

        /// <summary>
        /// Prepare IL to inject native code.
        /// </summary>
        /// <remarks>
        /// To inject native code, its necessary generate a IL which native code size generated by JIT
        /// is greater than or equals native code size to inject.
        /// </remarks>
        /// <param name="methodContext">Context to prepare IL.</param>
        /// <returns>Address and size of IL.</returns>
        private (IntPtr ilAddress, int ilLength) PrepareIL(MethodContext methodContext)
        {
            if (methodContext == null)
                throw new ArgumentNullException(nameof(methodContext));

            if (methodContext.NativeCode == null)
                throw new NullReferenceException(nameof(methodContext.NativeCode));

            System.Reflection.MethodInfo method = (System.Reflection.MethodInfo)methodContext.Method;

            int metadataToken;

            if (method.IsGenericMethod)
            {
                metadataToken = 0x2B000001;
            }
            else
            {
                metadataToken = method.MetadataToken;
            }

            byte[] tokenBytes =
            {
                (byte) metadataToken,
                (byte) (metadataToken >> 8),
                (byte) (metadataToken >> 16),
                (byte) (metadataToken >> 24)
            };

            List<byte> callBody = new List<byte>();

            bool isVoid = method.ReturnType == typeof(void);

            int argIndex = 0;

            if (!method.IsStatic)
            {
                argIndex++;
                callBody.Add((byte)OpCodes.Ldarg_0.Value);
            }

            int totalArgs = method.GetParameters().Count(w => !w.IsOptional);

            for (int i = 0; i < totalArgs; i++)
            {
                callBody.Add((byte)OpCodes.Ldarga_S.Value);
                callBody.Add((byte)argIndex++);
            }

            callBody.Add((byte)OpCodes.Call.Value);
            callBody.AddRange(tokenBytes);

            if (!isVoid)
                callBody.Add((byte)OpCodes.Pop.Value);

            byte[] callBytes = callBody.ToArray();

            int bodyLength = (int)Math.Ceiling((double)methodContext.NativeCode.Length / callBytes.Length) * callBytes.Length;
            int retLength = 1;

            if (!isVoid)
                retLength = callBytes.Length;

            int ilSize = bodyLength + retLength;

            IntPtr ilAddress = Marshal.AllocHGlobal(ilSize);

            for (int i = 0; i < bodyLength; i += callBytes.Length)
            {
                Marshal.Copy(callBytes, 0, ilAddress + i, callBytes.Length);
            }

            if (!isVoid)
            {
                Marshal.Copy(callBytes, 0, ilAddress + bodyLength, callBytes.Length);

            }

            Marshal.WriteByte(ilAddress + ilSize - 1, (byte)OpCodes.Ret.Value);

            return (ilAddress, ilSize);
        }

        public void Dispose()
        {
            lock (JitLock)
            {
                if (_isDisposed)
                    return;

                if (IsEnabled)
                {
                    _hookManager.RemoveHook(_resolveToken);
                    _hookManager.RemoveHook(_constructStringLiteral);
                    _hookManager.RemoveHook(_compileMethod);
                }

                _methodResolvers = null;
                _tokenResolvers = null;
                _constructStringLiteral = null;

                _compileMethod = null;
                _resolveToken = null;
                _constructStringLiteral = null;

                _instance = null;
                _isDisposed = true;
                IsEnabled = false;

            }

            GC.SuppressFinalize(this);
        }

    }
}