using Jitex.Hook;
using Jitex.JIT.CorInfo;
using Jitex.Utils;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Jitex.Framework;
using Jitex.Framework.Offsets;
using Jitex.JIT.Context;
using Jitex.Runtime;
using Microsoft.Extensions.Logging;
using MethodBody = Jitex.Builder.Method.MethodBody;
using MethodInfo = Jitex.JIT.CorInfo.MethodInfo;
using static Jitex.JIT.JitexHandler;
using static Jitex.Utils.JitexLogger;
using Jitex.JIT.Handlers;
using Jitex.Utils.NativeAPI.Windows;
using Mono.Unix.Native;
using static Jitex.Utils.MemoryHelper;

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
        /// <param name="methodCompiled">Method compiled.</param>
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

        private event MethodCompiledHandler? OnMethodCompiled;

        private MethodResolverHandler? _methodResolvers;

        private TokenResolverHandler? _tokenResolvers;

        public bool IsEnabled { get; private set; }

        /// <summary>
        ///     Prepare custom JIT.
        /// </summary>
        private ManagedJit()
        {
            ModuleHelper.Initialize();
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
            RuntimeHelperExtension.PrepareDelegate(_compileMethod, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, (uint)0,
                IntPtr.Zero, 0);

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

        internal void AddOnMethodCompiledEvent(MethodCompiledHandler handler) => OnMethodCompiled += handler;
        internal void RemoveOnMethodCompiledEvent(MethodCompiledHandler handler) => OnMethodCompiled -= handler;

        internal bool HasMethodResolver(MethodResolverHandler methodResolver) => _methodResolvers != null &&
                                                                                 _methodResolvers.GetInvocationList().Any(del => del.Method == methodResolver.Method);

        internal bool HasTokenResolver(TokenResolverHandler tokenResolver) => _tokenResolvers != null &&
                                                                              _tokenResolvers.GetInvocationList()
                                                                                  .Any(del => del.Method ==
                                                                                              tokenResolver.Method);


        /// <summary>
        /// Enable Jitex hooks
        /// </summary>
        internal void Enable()
        {
            lock (JitLock)
            {
                if (IsEnabled)
                    return;

                _hookManager.InjectHook(_framework.ICorJitCompileVTable, _compileMethod);

                if (_framework.CEEInfoVTable != IntPtr.Zero)
                {
                    _hookManager.InjectHook(CEEInfo.ResolveTokenIndex, _resolveToken);
                    _hookManager.InjectHook(CEEInfo.ConstructStringLiteralIndex, _constructStringLiteral);
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

                _hookManager.RemoveHook(_compileMethod);

                if (_framework.CEEInfoVTable != IntPtr.Zero)
                {
                    _hookManager.RemoveHook(_resolveToken);
                    _hookManager.RemoveHook(_constructStringLiteral);
                }
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
        private CorJitResult CompileMethod(IntPtr thisPtr, IntPtr comp, IntPtr info, uint flags, IntPtr nativeEntry,
            out int nativeSizeOfCode)
        {
            using IDisposable compileMethodScope = Log?.BeginScope("CompileMethod")!;

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
                    return _framework.CompileMethod(thisPtr, comp, info, flags, nativeEntry, out nativeSizeOfCode);

                MethodInfo methodInfo = new MethodInfo(info);
                MethodBase? methodFound = MethodHelper.GetMethodFromHandle(methodInfo.MethodHandle);

                if (methodFound == null)
                {
                    Log?.LogTrace(
                        $"Method for handle: {methodInfo.MethodHandle} not found. Calling original CompileMethod...");
                    return _framework.CompileMethod(thisPtr, comp, info, flags, nativeEntry, out nativeSizeOfCode);
                }

                if (DynamicHelpers.IsDynamicScope(methodInfo.Scope))
                {
                    Log?.LogDebug("Is a dynamic scope, getting owner...");
                    methodFound = DynamicHelpers.GetOwner(methodFound);
                }

                using IDisposable methodScope = Log?.BeginScope(methodFound.ToString())!;
                Log?.LogInformation($"Method to be compiled: {methodFound}");

                Delegate[] resolvers = _methodResolvers == null
                    ? Array.Empty<Delegate>()
                    : _methodResolvers.GetInvocationList();

                if (resolvers.Any())
                {
                    lock (JitLock)
                    {
                        if (_framework.CEEInfoVTable == IntPtr.Zero)
                        {
                            Log?.LogTrace("Reading CEEInfoVTable...");
                            _framework.ReadICorJitInfoVTable(comp);

                            Log?.LogTrace("Injecting hook for ResolveToken");
                            _hookManager.InjectHook(CEEInfo.ResolveTokenIndex, _resolveToken);
                            Log?.LogTrace("Injecting hook for ConstructStringLiteralIndex");
                            _hookManager.InjectHook(CEEInfo.ConstructStringLiteralIndex, _constructStringLiteral);
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
                            Log?.LogInformation(
                                $"Calling resolver [{resolver.Method.DeclaringType?.FullName}.{resolver.Method.Name}]");

                            resolver(methodContext);
                        }
                        catch (Exception ex)
                        {
                            Log?.LogError(ex,
                                $"Failed to execute resolver [{resolver.Method.DeclaringType?.FullName}.{resolver.Method.Name}].");
                        }

                        if (methodContext.IsResolved)
                        {
                            Log?.LogInformation(
                                $"Method resolved by [{resolver.Method.DeclaringType?.FullName}.{resolver.Method.Name}]");
                            break;
                        }
                    }

                    Log?.LogDebug(
                        $"Is method resolved: {methodContext.IsResolved}. ResolveMode: {methodContext.Mode.ToString()}");

                    _tokenTls = new TokenTls();

                    if (methodContext.IsResolved && methodContext.Mode.HasFlag(MethodContext.ResolveMode.IL))
                    {
                        MethodBody methodBody = methodContext.Body;

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
                        methodInfo.ILCode = MarshalHelper.CreateArrayCopy(methodBody.IL);
                        methodInfo.ILCodeSize = (uint)methodBody.IL.Length;
                    }
                }

                var result = _framework.CompileMethod(thisPtr, comp, info, flags, nativeEntry, out nativeSizeOfCode);

                if (result != CorJitResult.CORJIT_OK)
                {
                    Log?.LogCritical($"Result from original compileMethod: {result}");
                    return result;
                }

                var realNativeEntry = Read<IntPtr>(nativeEntry);

                MethodCompiled methodCompiled = new(methodFound, methodContext, methodInfo, result, realNativeEntry,
                    nativeSizeOfCode);

                RuntimeMethodCache.AddMethod(methodCompiled);
                OnMethodCompiled?.Invoke(methodCompiled);

                if (ilAddress != IntPtr.Zero)
                    Marshal.FreeHGlobal(ilAddress);

                if (sigAddress != IntPtr.Zero)
                    Marshal.FreeHGlobal(sigAddress);

                if (methodContext is not { IsResolved: true })
                    return result;

                if (methodContext.Mode == MethodContext.ResolveMode.Native)
                {
                    Log?.LogDebug("Overwriting generated native code...");

                    WriteNative(methodContext.NativeCode!, ref nativeSizeOfCode, nativeEntry);

                    Log?.LogDebug("Native code overwrited.");
                }
                else if (methodContext.Mode == MethodContext.ResolveMode.Entry)
                {
                    Log?.LogDebug($"Overwriting original EntryPoint...");

                    var entryContext = methodContext.EntryContext!;

                    WriteEntry(entryContext, ref nativeSizeOfCode, nativeEntry);

                    methodCompiled.NativeCode.Address = nativeEntry;
                    methodCompiled.NativeCode.Size = nativeSizeOfCode;

                    Log?.LogDebug("EntryPoint overwrited.");
                }

                return result;
            }
            catch (Exception ex)
            {
                Log?.LogCritical(ex, "Failed to compile method.");
                nativeSizeOfCode = default;
                throw new Exception("Failed compile method.", ex);
            }
            finally
            {
                _compileTls.EnterCount--;
            }
        }

        private static void WriteEntry(NativeCode nativeCode, ref int nativeSize, IntPtr nativeEntry)
        {
            Write(nativeEntry, nativeCode.Address);

            if (nativeCode.Size > 0)
                nativeSize = nativeCode.Size;
        }

        private static void WriteNative(byte[] nativeCode, ref int nativeSize, IntPtr nativeEntry)
        {
            var size = nativeCode.Length;
            var address = Marshal.AllocHGlobal(size);

            unsafe
            {
                var ptr = Unsafe.AsPointer(ref nativeCode[0]);
                Unsafe.CopyBlock(address.ToPointer(), ptr, (uint)size);
            }

            Write(nativeEntry, address);
            nativeSize = size;

            if (OSHelper.IsWindows)
            {
                Kernel32.VirtualProtect(address, size, Kernel32.MemoryProtection.EXECUTE_READ_WRITE);
            }
            else
            {
                var (alignedAddress, alignedSize) = GetAlignedAddress(address, size);

                if (OSHelper.IsHardenedRuntime)
                    Syscall.mprotect(alignedAddress, alignedSize, MmapProts.PROT_READ | MmapProts.PROT_EXEC);
                else
                    Syscall.mprotect(alignedAddress, alignedSize, MmapProts.PROT_READ | MmapProts.PROT_WRITE | MmapProts.PROT_EXEC);
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

            int token = 0;

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
                token = resolvedToken.Token; //Just to show on exception.
                IntPtr sourceAddress = Marshal.ReadIntPtr(thisHandle, IntPtr.Size * ResolvedTokenOffset.SourceOffset);
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
            catch (Exception ex)
            {
                throw new Exception($"Failed to resolve token: 0x{token:X}.", ex);
            }
            finally
            {
                _tokenTls.EnterCount--;
            }
        }

        private InfoAccessType ConstructStringLiteral(IntPtr thisHandle, IntPtr hModule, int metadataToken,
            IntPtr ppValue)
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

                            var result = CEEInfo.ConstructStringLiteral(thisHandle, hModule, metadataToken, ppValue);
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

        public void Dispose()
        {
            lock (JitLock)
            {
                if (_isDisposed)
                    return;

                Disable();

                _methodResolvers = null;
                _tokenResolvers = null;

                _instance = null;
                _isDisposed = true;
                IsEnabled = false;
            }

            GC.SuppressFinalize(this);
        }
    }
}