using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Jitex.Framework;
using Jitex.JIT.CorInfo;
using Jitex.JIT.Handlers;
using Jitex.JIT.Hooks.String;
using Jitex.JIT.Hooks.Token;
using Jitex.Runtime;
using Jitex.Utils;
using Jitex.Utils.NativeAPI.Windows;
using Mono.Unix.Native;
using MethodInfo = Jitex.JIT.CorInfo.MethodInfo;

namespace Jitex.JIT.Hooks.CompileMethod;

/// <summary>
/// Method resolver handler.
/// </summary>
/// <param name="context">Context of method.</param>
public delegate void MethodResolverHandler(MethodContext context);

/// <summary>
/// Handler to event after compiled method.
/// </summary>
/// <param name="methodCompiled">Method compiled.</param>
public delegate void MethodCompiledHandler(MethodCompiled methodCompiled);

internal class CompileMethodHook : HookBase<RuntimeFramework.CompileMethodDelegate>
{
    private static CompileMethodHook? Instance { get; set; }
    private static readonly ConcurrentDictionary<IntPtr, MethodBase?> HandleSource = new();
    private event MethodCompiledHandler? OnMethodCompiled;

    internal void AddOnMethodCompiledEvent(MethodCompiledHandler handler) => OnMethodCompiled += handler;
    internal void RemoveOnMethodCompiledEvent(MethodCompiledHandler handler) => OnMethodCompiled -= handler;


    private CompileMethodHook() : base(Hook)
    {
    }

    public static CompileMethodHook GetInstance()
    {
        Instance ??= new CompileMethodHook();
        return Instance;
    }

    /// <summary>
    /// Lock to prevent unload in compile time.
    /// </summary>
    private static readonly object JitLock = new object();

    /// <summary>
    ///     Wrap delegate to compileMethod from ICorJitCompiler.
    /// </summary>
    /// <param name="thisPtr">this parameter (pointer to CILJIT).</param>
    /// <param name="comp">(IN) - Pointer to ICorJitInfo.</param>
    /// <param name="info">(IN) - Pointer to CORINFO_METHOD_INFO.</param>
    /// <param name="flags">(IN) - Pointer to CorJitFlag.</param>
    /// <param name="nativeEntry">(OUT) - Pointer to NativeEntry.</param>
    /// <param name="nativeSizeOfCode">(OUT) - Size of NativeEntry.</param>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static CorJitResult Hook(IntPtr thisPtr, IntPtr comp, IntPtr info, uint flags, IntPtr nativeEntry,
        out int nativeSizeOfCode)
    {
        Tls ??= new ThreadTls();

        if (thisPtr == default)
        {
            nativeSizeOfCode = 0;
            return 0;
        }

        Tls.EnterCount++;

        try
        {
            var sigAddress = IntPtr.Zero;
            var ilAddress = IntPtr.Zero;

            //Dont put anything inside "if" to be compiled! Otherwise, will raise a StackOverflow
            if (Tls.EnterCount > 1)
                return Framework.CompileMethod(thisPtr, comp, info, flags, nativeEntry, out nativeSizeOfCode);

            var methodInfo = new MethodInfo(info);
            var methodFound = MethodHelper.GetMethodFromHandle(methodInfo.MethodHandle);

            if (methodFound == null)
                return Framework.CompileMethod(thisPtr, comp, info, flags, nativeEntry, out nativeSizeOfCode);

            if (DynamicHelpers.IsDynamicScope(methodInfo.Scope))
            {
                methodFound = DynamicHelpers.GetOwner(methodFound);
            }

            if (Framework.CEEInfoVTable == IntPtr.Zero)
            {
                lock (JitLock)
                {
                    if (Framework.CEEInfoVTable == IntPtr.Zero)
                    {
                        Framework.ReadICorJitInfoVTable(comp);

                        Token.TokenHook.GetInstance().InjectHook(CEEInfo.ResolveTokenIndex);
                        String.StringHook.GetInstance().InjectHook(CEEInfo.ConstructStringLiteralIndex);
                    }
                }
            }

            //Try retrieve source from call.
            //---
            //Before method to be compiled, he should be "resolved" (resolveToken).
            //Inside resolveToken, we can get source (which requested compilation) and destiny handle method (which be compiled).
            //In theory, every method to be compiled, should pass inside resolveToken, but has some unknown cases which they will be not "resolved".
            //Also, this is an inaccurate way to get source, because in some cases, can return a false source.
            var hasSource = HandleSource.TryGetValue(methodInfo.MethodHandle, out var source);

            var methodContext = new MethodContext(methodFound, source, hasSource);

            foreach (var handler in GetInvocationList<MethodResolverHandler>())
            {
                try
                {
                    handler(methodContext);
                }
                catch (Exception ex)
                {
                    continue;
                }

                if (methodContext.IsResolved)
                    break;
            }

            Tls = new ThreadTls();

            if (methodContext.Mode == MethodContext.ResolveMode.IL)
            {
                var methodBody = methodContext.Body;

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

            var result = Framework.CompileMethod(thisPtr, comp, info, flags, nativeEntry, out nativeSizeOfCode);

            if (result != CorJitResult.CORJIT_OK)
                return result;

            var realNativeEntry = MemoryHelper.Read<IntPtr>(nativeEntry);

            MethodCompiled methodCompiled = new(methodFound, methodContext, methodInfo, result, realNativeEntry,
                nativeSizeOfCode);

            RuntimeMethodCache.AddMethod(methodCompiled);
            // OnMethodCompiled?.Invoke(methodCompiled);

            if (ilAddress != IntPtr.Zero)
                Marshal.FreeHGlobal(ilAddress);

            if (sigAddress != IntPtr.Zero)
                Marshal.FreeHGlobal(sigAddress);

            if (methodContext is not { IsResolved: true })
                return result;

            if (methodContext.Mode == MethodContext.ResolveMode.Native)
            {
                WriteNative(methodContext.NativeCode!, ref nativeSizeOfCode, nativeEntry);
            }
            else if (methodContext.Mode == MethodContext.ResolveMode.Entry)
            {
                var entryContext = methodContext.EntryContext!;

                WriteEntry(entryContext, ref nativeSizeOfCode, nativeEntry);

                methodCompiled.NativeCode.Address = nativeEntry;
                methodCompiled.NativeCode.Size = nativeSizeOfCode;
            }

            return result;
        }
        catch (Exception ex)
        {
            nativeSizeOfCode = default;
            throw new Exception("Failed compile method.", ex);
        }

        finally
        {
            Tls.EnterCount--;
        }
    }

    private static void WriteEntry(NativeCode nativeCode, ref int nativeSize, IntPtr nativeEntry)
    {
        MemoryHelper.Write(nativeEntry, nativeCode.Address);

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

        MemoryHelper.Write(nativeEntry, address);
        nativeSize = size;

        if (OSHelper.IsWindows)
        {
            Kernel32.VirtualProtect(address, size, Kernel32.MemoryProtection.EXECUTE_READ_WRITE);
        }
        else
        {
            var (alignedAddress, alignedSize) = MemoryHelper.GetAlignedAddress(address, size);

            if (OSHelper.IsHardenedRuntime)
                Syscall.mprotect(alignedAddress, alignedSize, MmapProts.PROT_READ | MmapProts.PROT_EXEC);
            else
                Syscall.mprotect(alignedAddress, alignedSize,
                    MmapProts.PROT_READ | MmapProts.PROT_WRITE | MmapProts.PROT_EXEC);
        }
    }

    internal static void RegisterSource(IntPtr methodHandle, MethodBase? source)
    {
        HandleSource.AddOrUpdate(methodHandle, source, (_, _) => source);
    }
}