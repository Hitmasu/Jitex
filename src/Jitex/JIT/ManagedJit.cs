using Jitex.Hook;
using Jitex.JIT.CorInfo;
using Jitex.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Jitex.Framework;
using Jitex.JIT.Context;
using Jitex.Runtime;
using Jitex.Utils.Extension;
using Microsoft.Extensions.Logging;
using MethodBody = Jitex.Builder.Method.MethodBody;
using MethodInfo = Jitex.JIT.CorInfo.MethodInfo;
using static Jitex.JIT.JitexHandler;
using static Jitex.Utils.JitexLogger;
using Jitex.JIT.Handlers;

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

        /// <summary>
        /// Handler to event after compiled method.
        /// </summary>
        /// <param name="code"></param>
        public delegate void MethodCompiledHandler(MethodCompiled methodCompiled);
    }

    /// <summary>
    /// Hook instance from JIT.
    /// </summary>
    internal sealed class ManagedJit : IDisposable
    {
        private readonly ConcurrentDictionary<IntPtr, MethodBase?> _handleSource = new();

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
        private RuntimeFramework.CompileMethodDelegate? _compileMethod;

        /// <summary>
        /// Custom resolve token.
        /// </summary>
        private CEEInfo.ResolveTokenDelegate? _resolveToken;

        /// <summary>
        /// Custom construct string literal.
        /// </summary>
        private CEEInfo.ConstructStringLiteralDelegate? _constructStringLiteral;

        private event MethodCompiledHandler _onMethodCompiled;

        private bool _isDisposed;

        private MethodResolverHandler? _methodResolvers;

        private TokenResolverHandler? _tokenResolvers;

        public bool IsEnabled { get; private set; }

        /// <summary>
        ///     Prepare custom JIT.
        /// </summary>
        private ManagedJit()
        {
            _framework = RuntimeFramework.Framework;

            _compileMethod = CompileMethod;
            _resolveToken = ResolveToken;
            _constructStringLiteral = ConstructStringLiteral;

            PrepareHook();

            _hookManager.InjectHook(_framework.ICorJitCompileVTable, _compileMethod);
            IsEnabled = true;
        }

        private void PrepareHook()
        {
            Log?.LogTrace("Preparing delegate for CompileMethod");
            RuntimeHelperExtension.PrepareDelegate(_compileMethod, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, (uint)0, IntPtr.Zero, 0);

            Log?.LogTrace("Preparing delegate for ResolveToken");
            RuntimeHelperExtension.PrepareDelegate(_resolveToken, IntPtr.Zero, IntPtr.Zero);

            Log?.LogTrace("Preparing delegate for ConstructStringLiteral");
            RuntimeHelperExtension.PrepareDelegate(_constructStringLiteral, IntPtr.Zero, IntPtr.Zero, 0, IntPtr.Zero);
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

        internal void AddOnMethodCompiledEvent(MethodCompiledHandler handler) => _onMethodCompiled += handler;
        internal void RemoveOnMethodCompiledEvent(MethodCompiledHandler handler) => _onMethodCompiled -= handler;

        internal bool HasMethodResolver(MethodResolverHandler methodResolver) => _methodResolvers != null && _methodResolvers.GetInvocationList().Any(del => del.Method == methodResolver.Method);

        internal bool HasTokenResolver(TokenResolverHandler tokenResolver) => _tokenResolvers != null && _tokenResolvers.GetInvocationList().Any(del => del.Method == tokenResolver.Method);
        #region Future Feature (Enable/Disable)

        /// <summary>
        /// Enable Jitex hooks
        /// </summary>
        internal void Enable()
        {
            lock (JitLock)
            {
                if (IsEnabled)
                    return;

                _hookManager.InjectHook(_framework.ICorJitCompileVTable, _compileMethod!);

                if (_framework.CEEInfoVTable != IntPtr.Zero)
                {
                    _hookManager.InjectHook(CEEInfo.ResolveTokenIndex, _resolveToken!);
                    _hookManager.InjectHook(CEEInfo.ConstructStringLiteralIndex, _constructStringLiteral!);
                }
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
                if (!IsEnabled)
                    return;

                _hookManager.RemoveHook(_compileMethod!);

                if (_framework.CEEInfoVTable != IntPtr.Zero)
                {
                    _hookManager.RemoveHook(_resolveToken!);
                    _hookManager.RemoveHook(_constructStringLiteral!);
                }
            }

            IsEnabled = false;
        }

        #endregion

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
            using var compileMethodScope = Log?.BeginScope("CompileMethod");

            _compileTls ??= new CompileTls();

            if (thisPtr == default)
            {
                nativeEntry = IntPtr.Zero;
                nativeSizeOfCode = 0;
                return 0;
            }

            _compileTls.EnterCount++;

            try
            {
                MethodContext? methodContext = null;
                IntPtr sigAddress = IntPtr.Zero;
                IntPtr ilAddress = IntPtr.Zero;

                //Dont put anything inside "if" to be compiled! Otherwise, will raise a StackOverflow
                if (_compileTls.EnterCount > 1)
                    return _framework.CompileMethod(thisPtr, comp, info, flags, out nativeEntry, out nativeSizeOfCode);

                MethodInfo methodInfo = new MethodInfo(info);
                MethodBase? methodFound = MethodHelper.GetMethodFromHandle(methodInfo.MethodHandle);

                if (methodFound == null)
                {
                    Log?.LogTrace($"Method for handle: {methodInfo.MethodHandle} not found. Calling original CompileMethod...");
                    return _framework.CompileMethod(thisPtr, comp, info, flags, out nativeEntry, out nativeSizeOfCode);
                }

                if (DynamicHelpers.IsDynamicScope(methodInfo.Scope))
                {
                    Log?.LogDebug("Is a dynamic scope, getting owner...");
                    methodFound = DynamicHelpers.GetOwner(methodFound);
                }

                using var methodScope = Log?.BeginScope(methodFound.ToString());
                Log?.LogInformation($"Method to be compiled: {methodFound}");

                Delegate[] resolvers = null!;

                if (_methodResolvers == null)
                    resolvers = new Delegate[0];
                else
                    resolvers = _methodResolvers.GetInvocationList();

                if (resolvers.Any())
                {
                    lock (JitLock)
                    {
                        if (_framework.CEEInfoVTable == IntPtr.Zero)
                        {
                            Log?.LogTrace("Reading CEEInfoVTable...");
                            _framework.ReadICorJitInfoVTable(comp);

                            Log?.LogTrace("Injecting hook for ResolveToken");
                            _hookManager.InjectHook(CEEInfo.ResolveTokenIndex, _resolveToken!);
                            Log?.LogTrace("Injecting hook for ConstructStringLiteralIndex");
                            _hookManager.InjectHook(CEEInfo.ConstructStringLiteralIndex, _constructStringLiteral!);
                        }
                    }

                    //Try retrieve source from call.
                    //---
                    //Before method to be compiled, he should be "resolved" (resolveToken).
                    //Inside resolveToken, we can get source (which requested compilation) and destiny handle method (which be compiled).
                    //In theory, every method to be compiled, should pass inside resolveToken, but has some unknown cases which they will be not "resolved".
                    //Also, this is an inaccurate way to get source, because in some cases, can return a false source.
                    bool hasSource = _handleSource.TryGetValue(methodInfo.MethodHandle, out MethodBase? source);

                    methodContext = new MethodContext(methodFound, source, hasSource);

                    foreach (MethodResolverHandler resolver in resolvers)
                    {
                        try
                        {
                            Log?.LogInformation($"Calling resolver [{resolver.Method.DeclaringType.FullName}.{resolver.Method.Name}]");
                            resolver(methodContext);
                        }
                        catch (Exception ex)
                        {
                            Log?.LogError(ex, $"Failed to execute resolver [{resolver.Method.DeclaringType.FullName}.{resolver.Method.Name}].");
                        }

                        if (methodContext.IsResolved)
                        {
                            Log?.LogInformation($"Method resolved by [{resolver.Method.DeclaringType.FullName}.{resolver.Method.Name}]");
                            break;
                        }
                    }

                    Log?.LogDebug($"Is method resolved: {methodContext.IsResolved}. ResolveMode: {methodContext.Mode.ToString()}");

                    _tokenTls = new TokenTls();

                    if (methodContext.IsResolved && (methodContext.Mode.HasFlag(MethodContext.ResolveMode.IL) || methodContext.Mode.HasFlag(MethodContext.ResolveMode.Native)))
                    {
                        int ilLength;

                        if (methodContext.Mode == MethodContext.ResolveMode.IL)
                        {
                            MethodBody methodBody = methodContext.Body;

                            ilLength = methodBody.IL.Length;

                            ilAddress = MarshalHelper.CreateArrayCopy(methodBody.IL);

                            if (methodBody.HasLocalVariable)
                            {
                                byte[] signatureVariables = methodBody.GetSignatureVariables();
                                sigAddress = MarshalHelper.CreateArrayCopy(signatureVariables);

                                methodInfo.Locals.Signature = sigAddress + 1;
                                methodInfo.Locals.Args = sigAddress + 3;
                                methodInfo.Locals.NumArgs = (ushort)methodBody.LocalVariables.Count;
                            }

                            methodInfo.MaxStack = methodBody.MaxStackSize;
                            methodInfo.EHCount = methodContext.Body.EHCount;
                        }
                        else
                        {
                            (ilAddress, ilLength) = PrepareIL(methodContext);

                            if (methodInfo.MaxStack < 8)
                                methodInfo.MaxStack = 8;
                        }

                        methodInfo.ILCode = ilAddress;
                        methodInfo.ILCodeSize = (uint)ilLength;
                    }
                }

                CorJitResult result = _framework.CompileMethod(thisPtr, comp, info, flags, out nativeEntry, out nativeSizeOfCode);

                if (result != CorJitResult.CORJIT_OK)
                    Log?.LogCritical($"Result from original compileMethod: {result}");

                MethodCompiled methodCompiled = new(methodFound, methodContext, methodInfo, result, nativeEntry, nativeSizeOfCode);
                RuntimeMethodCache.AddMethod(methodCompiled);
                _onMethodCompiled?.Invoke(methodCompiled);

                if (ilAddress != IntPtr.Zero)
                    Marshal.FreeHGlobal(ilAddress);

                if (sigAddress != IntPtr.Zero)
                    Marshal.FreeHGlobal(sigAddress);

                if (methodContext is { IsResolved: true })
                {
                    if (methodContext?.Mode == MethodContext.ResolveMode.Native)
                    {
                        Log?.LogDebug("Overwriting generated native code...");
                        Marshal.Copy(methodContext.NativeCode!, 0, nativeEntry, methodContext.NativeCode!.Length);
                        Log?.LogDebug("Native code overwrited.");
                    }
                    else if (methodContext?.Mode == MethodContext.ResolveMode.Detour)
                    {
                        Log?.LogDebug("Detouring method...");
                        DetourContext detourContext = methodContext.DetourContext!;
                        detourContext.MethodAddress = nativeEntry;
                        detourContext.Enable();
                        Log?.LogDebug("Method detoured.");
                    }
                    else if (methodContext?.Mode == MethodContext.ResolveMode.Entry)
                    {
                        NativeCode entryContext = methodContext.EntryContext!;
                        nativeEntry = entryContext.Address;

                        Log?.LogDebug($"Overwriting original EntryPoint...");

                        if (entryContext.Size > 0)
                            nativeSizeOfCode = entryContext.Size;

                        methodCompiled.NativeCode.Address = nativeEntry;
                        methodCompiled.NativeCode.Size = nativeSizeOfCode;

                        Log?.LogDebug("EntryPoint overwrited.");
                    }
                    else if (methodContext?.Mode == MethodContext.ResolveMode.Intercept)
                    {
                        Log?.LogDebug("Creating context to intercept method...");
                        //To make intercept possible, we need compile method 2 times:
                        //1º method it's method will be detoured
                        //2º method it's our unmodified method.
                        //This way, make easy turn on/off interception call.

                        //Compile method again to get a second address (like a clone)
                        _framework.CompileMethod(thisPtr, comp, info, flags, out IntPtr secondaryNativeEntry, out _);

                        InterceptContext interceptContext = methodContext.InterceptContext;

                        //It's necessary save address from original to be called later (in case interceptor needs call original method) 
                        interceptContext.MethodOriginalAddress = nativeEntry;

                        //Address which will be detoured (this will be the trampoline to our intercept method).
                        interceptContext.MethodTrampolineAddress = secondaryNativeEntry;

                        //Set trampoline to be method native address
                        nativeEntry = secondaryNativeEntry;
                        //Write detour on method.
                        Intercept.InterceptManager.GetInstance().AddIntercept(interceptContext);

                        //That's how should work:
                        //CallerMethod -> Detour Method -> Intercept Method -> Safe Method (MethodAddress)
                        Log?.LogDebug("Method intercepted.");
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                Log?.LogCritical(ex, "Failed to compile method.");
                nativeEntry = default;
                nativeSizeOfCode = default;
                return 0;
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
                _handleSource.AddOrUpdate(IntPtr.Zero, MethodBase.GetCurrentMethod(), (ptr, b) => null);
                return;
            }

            try
            {
                if (_tokenTls.EnterCount > 1 || _tokenResolvers == null)
                {
                    CEEInfo.ResolveToken(thisHandle, pResolvedToken);
                    return;
                }

                Delegate[] resolvers = _tokenResolvers.GetInvocationList();

                if (!resolvers.Any())
                {
                    CEEInfo.ResolveToken(thisHandle, pResolvedToken);
                    return;
                }

                ResolvedToken resolvedToken = new ResolvedToken(pResolvedToken);

                IntPtr sourceAddress = Marshal.ReadIntPtr(thisHandle, IntPtr.Size * 2);
                MethodBase? source = MethodHelper.GetMethodFromHandle(sourceAddress);
                bool hasSource = source != null;

                TokenContext context = new TokenContext(ref resolvedToken, source, hasSource);

                foreach (TokenResolverHandler resolver in resolvers)
                {
                    resolver(context);
                }

                CEEInfo.ResolveToken(thisHandle, pResolvedToken);

                if (resolvedToken.HMethod != IntPtr.Zero)
                {
                    if (!_handleSource.TryGetValue(resolvedToken.HMethod, out MethodBase? _))
                    {
                        _handleSource[resolvedToken.HMethod] = source;
                    }
                }
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
                    Delegate[] resolvers = _tokenResolvers.GetInvocationList();

                    if (!resolvers.Any())
                        return CEEInfo.ConstructStringLiteral(thisHandle, hModule, metadataToken, ppValue);

                    ConstructString constructString = new ConstructString(hModule, metadataToken);
                    TokenContext context = new TokenContext(constructString, null, false);

                    foreach (TokenResolverHandler resolver in resolvers)
                    {
                        resolver(context);

                        if (context.IsResolved)
                        {
                            if (string.IsNullOrEmpty(context.Content))
                                throw new ArgumentNullException("String content can't be null or empty.");

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
        private static (IntPtr ilAddress, int ilLength) PrepareIL(MethodContext methodContext)
        {
            if (methodContext == null)
                throw new ArgumentNullException(nameof(methodContext));

            if (methodContext.NativeCode == null)
                throw new NullReferenceException(nameof(methodContext.NativeCode));

            System.Reflection.MethodInfo method = (System.Reflection.MethodInfo)methodContext.Method;

            int metadataToken = method.IsGenericMethod ? 0x2B000001 : method.MetadataToken;

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
                    // _hookManager.RemoveHook(_resolveToken!);
                    // _hookManager.RemoveHook(_constructStringLiteral!);
                    // _hookManager.RemoveHook(_compileMethod!);
                    Disable();
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