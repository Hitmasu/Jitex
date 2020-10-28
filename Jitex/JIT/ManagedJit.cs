using Jitex.Hook;
using Jitex.JIT.CorInfo;
using Jitex.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Jitex.Builder.IL;
using Jitex.Exceptions;
using Jitex.JIT.Context;
using Jitex.Runtime;
using Jitex.Utils.Extension;
using static Jitex.JIT.JitexHandler;
using LocalVariableInfo = Jitex.Builder.Method.LocalVariableInfo;
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
        private readonly HookManager _hookManager = new HookManager();

        /// <summary>
        /// Running framework.
        /// </summary>
        private static readonly RuntimeFramework Framework;

        /// <summary>
        /// Lock to prevent multiple instance.
        /// </summary>
        private static readonly object InstanceLock = new object();

        /// <summary>
        /// Lock to prevent unload in compile time.
        /// </summary>
        private static readonly object JitLock = new object();

        /// <summary>
        /// Custom compíle method.
        /// </summary>
        private CorJitCompiler.CompileMethodDelegate _compileMethod;

        /// <summary>
        /// Custom resolve token.
        /// </summary>
        private CEEInfo.ResolveTokenDelegate _resolveToken;

        /// <summary>
        /// Custom construct string literal.
        /// </summary>
        private CEEInfo.ConstructStringLiteralDelegate _constructStringLiteral;

        private bool _isDisposed;

        [ThreadStatic] private static CompileTls? _compileTls;

        [ThreadStatic] private static TokenTls? _tokenTls;

        private static ManagedJit? _instance;

        private MethodResolverHandler? _methodResolvers;

        private TokenResolverHandler? _tokenResolvers;

        public static bool IsLoaded => _instance != null;

        static ManagedJit()
        {
            Framework = RuntimeFramework.GetFramework();
        }

        internal void AddMethodResolver(MethodResolverHandler methodResolver) => _methodResolvers += methodResolver;

        internal void AddTokenResolver(TokenResolverHandler tokenResolver) => _tokenResolvers += tokenResolver;

        internal void RemoveMethodResolver(MethodResolverHandler methodResolver) => _methodResolvers -= methodResolver;

        internal void RemoveTokenResolver(TokenResolverHandler tokenResolver) => _tokenResolvers -= tokenResolver;

        internal bool HasMethodResolver(MethodResolverHandler methodResolver) => _methodResolvers != null && _methodResolvers.GetInvocationList().Any(del => del.Method == methodResolver.Method);

        internal bool HasTokenResolver(TokenResolverHandler tokenResolver) => _tokenResolvers != null && _tokenResolvers.GetInvocationList().Any(del => del.Method == tokenResolver.Method);

        /// <summary>
        ///     Prepare custom JIT.
        /// </summary>
        private ManagedJit()
        {
            _compileMethod = CompileMethod;
            _resolveToken = ResolveToken;
            _constructStringLiteral = ConstructStringLiteral;

            RuntimeHelperExtension.PrepareDelegate(_compileMethod, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, (uint)0, IntPtr.Zero, 0);
            RuntimeHelperExtension.PrepareDelegate(_resolveToken, IntPtr.Zero, IntPtr.Zero);
            RuntimeHelperExtension.PrepareDelegate(_constructStringLiteral, IntPtr.Zero, IntPtr.Zero, 0, IntPtr.Zero);

            _hookManager.InjectHook(Framework.ICorJitCompileVTable, _compileMethod);
        }

        /// <summary>
        /// Get singleton instance from ManagedJit.
        /// </summary>
        /// <returns></returns>
        internal static ManagedJit GetInstance()
        {
            lock (InstanceLock)
            {
                _instance ??= new ManagedJit();
                return _instance;
            }
        }

        /// <summary>
        ///     Wrap delegate to compileMethod from ICorJitCompiler.
        /// </summary>
        /// <param name="thisPtr">this parameter.</param>
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

            CompileTls compileEntry = _compileTls ??= new CompileTls();
            compileEntry.EnterCount++;

            try
            {
                MethodContext? methodContext = null;
                IntPtr sigAddress = IntPtr.Zero;
                IntPtr ilAddress = IntPtr.Zero;

                if (compileEntry.EnterCount == 1 && _methodResolvers != null)
                {
                    IEnumerable<Delegate> resolvers = _methodResolvers.GetInvocationList();

                    if (resolvers.Any())
                    {
                        MethodInfo methodInfo = new MethodInfo(info);

                        lock (JitLock)
                        {
                            if (Framework.CEEInfoVTable == IntPtr.Zero)
                            {
                                Framework.ReadICorJitInfoVTable(comp);

                                _hookManager.InjectHook(CEEInfo.ResolveTokenIndex, _resolveToken);
                                _hookManager.InjectHook(CEEInfo.ConstructStringLiteralIndex, _constructStringLiteral);
                            }
                        }

                        if (methodInfo.Module != null)
                        {
                            uint methodToken = CEEInfo.GetMethodDefFromMethod(methodInfo.MethodDesc);
                            MethodBase methodFound = methodInfo.Module.ResolveMethod((int)methodToken);

                            _tokenTls = new TokenTls { Root = methodFound };

                            methodContext = new MethodContext(methodFound);

                            foreach (MethodResolverHandler resolver in resolvers)
                            {
                                resolver(methodContext);

                                if (methodContext.IsResolved)
                                    break;
                            }
                        }

                        if (methodContext != null && methodContext.IsResolved)
                        {
                            int ilLength;

                            if (methodContext.Mode == MethodContext.ResolveMode.IL)
                            {
                                MethodBody methodBody = methodContext.MethodBody;

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
                                (ilAddress, ilLength) = PrepareIL(methodContext);

                                if (methodInfo.MaxStack < 8)
                                    methodInfo.MaxStack = 8;
                            }

                            methodInfo.ILCode = ilAddress;
                            methodInfo.ILCodeSize = (uint)ilLength;
                        }
                    }
                }

                CorJitResult result = Framework.CorJitCompiler.CompileMethod(thisPtr, comp, info, flags, out nativeEntry, out nativeSizeOfCode);

                if (ilAddress != IntPtr.Zero && methodContext!.Mode == MethodContext.ResolveMode.IL)
                    Marshal.FreeHGlobal(ilAddress);

                if (sigAddress != IntPtr.Zero)
                    Marshal.FreeHGlobal(sigAddress);

                if (methodContext?.Mode == MethodContext.ResolveMode.NATIVE)
                {
                    Marshal.Copy(methodContext.NativeCode!, 0, nativeEntry, methodContext.NativeCode!.Length);
                }

                return result;
            }
            finally
            {
                compileEntry.EnterCount--;
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

                    //Capture method who trying resolve that token.
                    _tokenTls.Source = _tokenTls.GetSource();

                    ResolvedToken resolvedToken = new ResolvedToken(pResolvedToken);
                    TokenContext context = new TokenContext(ref resolvedToken, _tokenTls.Source);

                    foreach (TokenResolverHandler resolver in resolvers)
                    {
                        resolver(context);
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
                    _tokenTls.Source = _tokenTls.GetSource();

                    ConstructString constructString = new ConstructString(hModule, metadataToken, ppValue);
                    TokenContext context = new TokenContext(constructString, _tokenTls.Source);

                    foreach (TokenResolverHandler resolver in resolvers)
                    {
                        resolver(context);

                        if (context.IsResolved)
                        {
                            if (string.IsNullOrEmpty(context.Content))
                                throw new StringNullOrEmptyException();

                            InfoAccessType result = CEEInfo.ConstructStringLiteral(thisHandle, hModule, metadataToken, ppValue);
                            WriteString(ppValue, context.Content);
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

            Assembly? entryAssembly = Assembly.GetEntryAssembly();

            if (entryAssembly == null)
                entryAssembly = Assembly.GetCallingAssembly();

            System.Reflection.MethodInfo main = entryAssembly.EntryPoint;
            int mainToken = main.MetadataToken;

            byte[] callIL =
            {
                (byte) OpCodes.Call.Value,
                (byte) mainToken,
                (byte) (mainToken >> 8),
                (byte) (mainToken >> 16),
                (byte) (mainToken >> 24)
            };

            byte[] retIL = PrepareReturn(methodContext.Method);

            int bodyLength = (int)Math.Ceiling((double)methodContext.NativeCode.Length / callIL.Length) * callIL.Length;
            int retLength = retIL.Length;
            int ilSize = bodyLength + retLength;
            IntPtr ilAddress = Marshal.AllocHGlobal(ilSize);

            for (int i = 0; i < bodyLength; i += callIL.Length)
            {
                Marshal.Copy(callIL, 0, ilAddress + i, callIL.Length);
            }

            Marshal.Copy(retIL, 0, ilAddress + bodyLength, retIL.Length);

            return (ilAddress, ilSize);
        }

        /// <summary>
        /// Prepare return of IL.
        /// </summary>
        /// <param name="method">Method to prepare.</param>
        /// <returns></returns>
        private static byte[] PrepareReturn(MethodBase method)
        {
            int methodToken = method.MetadataToken;

            IList<byte> retIL = new List<byte>();

            int argIndex = 0;

            if (!method.IsStatic)
            {
                argIndex++;
                retIL.Add((byte)OpCodes.Ldarg_0.Value);
            }

            int totalArgs = method.GetParameters().Count(w => !w.IsOptional);

            for (int i = 0; i < totalArgs; i++)
            {
                retIL.Add((byte)OpCodes.Ldarga_S.Value);
                retIL.Add((byte)argIndex++);
            }

            retIL.Add((byte)OpCodes.Call.Value);
            retIL.Add((byte)(methodToken));
            retIL.Add((byte)(methodToken >> 8));
            retIL.Add((byte)(methodToken >> 16));
            retIL.Add((byte)(methodToken >> 24));
            retIL.Add((byte)OpCodes.Ret.Value);

            return retIL.ToArray();
        }

        public void Dispose()
        {
            lock (JitLock)
            {
                if (_isDisposed)
                    return;

                _hookManager.RemoveHook(_resolveToken);
                _hookManager.RemoveHook(_compileMethod);
                _hookManager.RemoveHook(_constructStringLiteral);

                _methodResolvers = null;
                _tokenResolvers = null;
                _constructStringLiteral = null;

                _compileMethod = null;
                _resolveToken = null;
                _constructStringLiteral = null;

                _instance = null;
                _isDisposed = true;
            }

            GC.SuppressFinalize(this);
        }

    }
}