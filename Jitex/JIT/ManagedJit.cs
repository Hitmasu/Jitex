using Jitex.Hook;
using Jitex.JIT.CorInfo;
using Jitex.Utils;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using static Jitex.JIT.CorInfo.CEEInfo;
using static Jitex.JIT.CorInfo.CorJitCompiler;
using MethodBody = Jitex.Builder.MethodBody;

namespace Jitex.JIT
{
    /// <summary>
    ///     Detour current jit.
    /// </summary>
    /// <remarks>
    ///     Source: https://xoofx.com/blog/2018/04/12/writing-managed-jit-in-csharp-with-coreclr/
    /// </remarks>
    public class ManagedJit : IDisposable
    {
        [DllImport("clrjit.dll", CallingConvention = CallingConvention.StdCall, SetLastError = true, EntryPoint = "getJit", BestFitMapping = true)]
        private static extern IntPtr GetJit();

        private readonly HookManager _hookManager = new HookManager();

        /// <summary>
        /// Custom compíle method.
        /// </summary>
        private CompileMethodDelegate _compileMethod;

        /// <summary>
        /// Custom resolve token.
        /// </summary>
        private ResolveTokenDelegate _resolveToken;

        private bool _isDisposed;

        [ThreadStatic] private static CompileTls _compileTls;

        [ThreadStatic] private static TokenTls _tokenTls;

        private static readonly object JitLock;

        private static ManagedJit _instance;

        private static readonly IntPtr JitVTable;

        private static readonly CorJitCompiler Compiler;

        private static IntPtr _corJitInfoPtr = IntPtr.Zero;

        private static CEEInfo _ceeInfo;

        private ResolveCompileHandle _resolversCompile;

        private ResolveTokenHandle _resolversToken;

        public delegate void ResolveCompileHandle(CompileContext method);

        public delegate void ResolveTokenHandle(TokenContext token);

        static ManagedJit()
        {
            JitLock = new object();

            IntPtr jit = GetJit();

            JitVTable = Marshal.ReadIntPtr(jit);
            Compiler = Marshal.PtrToStructure<CorJitCompiler>(JitVTable);
        }

        public void AddCompileResolver(ResolveCompileHandle compileResolve)
        {
            _resolversCompile += compileResolve;
        }

        public void AddTokenResolver(ResolveTokenHandle tokenResolve)
        {
            _resolversToken += tokenResolve;
        }

        public void RemoveCompileResolver(ResolveCompileHandle compileResolve)
        {
            _resolversCompile -= compileResolve;
        }

        public void RemoveTokenResolver(ResolveTokenHandle tokenResolve)
        {
            _resolversToken -= tokenResolve;
        }

        /// <summary>
        ///     Prepare custom JIT.
        /// </summary>
        private ManagedJit()
        {
            if (Compiler.CompileMethod == null)
                return;

            _compileMethod = CompileMethod;
            _resolveToken = ResolveToken;

            CORINFO_METHOD_INFO emptyInfo = default;
            CORINFO_RESOLVED_TOKEN corinfoResolvedToken = default;

            RuntimeHelperExtension.PrepareDelegate(_resolveToken, IntPtr.Zero, corinfoResolvedToken);
            RuntimeHelperExtension.PrepareDelegate(_compileMethod, IntPtr.Zero, IntPtr.Zero, emptyInfo, (uint)0, IntPtr.Zero, 0);
            RuntimeHelpers.PrepareDelegate(_resolversToken);

            _hookManager.InjectHook(JitVTable, _compileMethod);
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
        private CorJitResult CompileMethod(IntPtr thisPtr, IntPtr comp, ref CORINFO_METHOD_INFO info, uint flags, out IntPtr nativeEntry, out int nativeSizeOfCode)
        {
            CompileTls compileEntry = _compileTls ??= new CompileTls();
            compileEntry.EnterCount++;

            try
            {
                if (thisPtr == default)
                {
                    nativeEntry = IntPtr.Zero;
                    nativeSizeOfCode = 0;
                    return 0;
                }

                CompileContext compileContext = null;
                IntPtr ilAddress = IntPtr.Zero;
                IntPtr sigAddress = IntPtr.Zero;

                if (compileEntry.EnterCount == 1 && _resolversCompile != null)
                {
                    lock (JitLock)
                    {
                        if (_corJitInfoPtr == IntPtr.Zero)
                        {
                            _corJitInfoPtr = Marshal.ReadIntPtr(comp);
                            _ceeInfo = new CEEInfo(_corJitInfoPtr);
                            _hookManager.InjectHook(_ceeInfo.ResolveTokenIndex, _resolveToken);
                        }
                    }

                    Module module = AppModules.GetModuleByPointer(info.scope);

                    if (module != null)
                    {
                        uint methodToken = _ceeInfo.GetMethodDefFromMethod(info.ftn);
                        MethodBase methodFound = module.ResolveMethod((int)methodToken);
                        _tokenTls = new TokenTls { Root = methodFound };

                        compileContext = new CompileContext(methodFound);

                        foreach (ResolveCompileHandle resolver in _resolversCompile.GetInvocationList())
                        {
                            resolver(compileContext);

                            //TODO
                            //Cascade resolvers
                            if (compileContext.IsResolved)
                                break;
                        }
                    }

                    if (compileContext != null && compileContext.IsResolved)
                    {
                        int ilLength;
                        

                        if (compileContext.Mode == CompileContext.ResolveMode.IL)
                        {
                            MethodBody methodBody = compileContext.MethodBody;

                            ilLength = methodBody.IL.Length;

                            ilAddress = Marshal.AllocHGlobal(methodBody.IL.Length);
                            Marshal.Copy(methodBody.IL, 0, ilAddress, methodBody.IL.Length);

                            if (methodBody.HasLocalVariable)
                            {
                                byte[] signatureVariables = methodBody.GetSignatureVariables();
                                sigAddress = Marshal.AllocHGlobal(signatureVariables.Length);
                                Marshal.Copy(signatureVariables, 0, sigAddress, signatureVariables.Length);

                                info.locals.pSig = sigAddress + 1;
                                info.locals.args = sigAddress + 3;
                                info.locals.numArgs = (ushort)methodBody.LocalVariables.Count;
                            }

                            info.maxStack = methodBody.MaxStackSize;
                        }
                        else
                        {
                            Span<byte> emptyBody;

                            //Min size byte-code generated by JIT
                            const int minSize = 21;

                            if (compileContext.ByteCode.Length > minSize)
                            {
                                //Calculate the size of IL to allocate byte-code
                                //Minimal size IL is 4 byte = JIT will compile to 21 bytes (byte-code)
                                //Upper that, for each bitwise operation (ldc.i4 + And) is generated 3 byte-code
                                //Example: 
                                //IL with 1 bitwise = 21 bytes
                                //IL with 2 bitwise = 24 bytes
                                //IL with 3 bitwise = 27 bytes
                                //...
                                int nextMax = compileContext.ByteCode.Length + (3 - compileContext.ByteCode.Length % 3);
                                ilLength = 4 + 2 * ((nextMax - minSize) / 3);

                                if (ilLength % 2 != 0)
                                {
                                    ilLength++;
                                }
                            }
                            else
                            {
                                ilLength = 4;
                            }

                            unsafe
                            {
                                void* ptr = stackalloc byte[ilLength];
                                emptyBody = new Span<byte>(ptr, ilLength);
                                ilAddress = new IntPtr(ptr);
                            }

                            //populate IL with bitwise operations
                            emptyBody[0] = (byte)OpCodes.Ldc_I4_1.Value;
                            emptyBody[1] = (byte)OpCodes.Ldc_I4_1.Value;
                            emptyBody[2] = (byte)OpCodes.And.Value;
                            emptyBody[^1] = (byte)OpCodes.Ret.Value;

                            for (int i = 3; i < emptyBody.Length - 2; i += 2)
                            {
                                emptyBody[i] = (byte)OpCodes.Ldc_I4_1.Value;
                                emptyBody[i + 1] = (byte)OpCodes.And.Value;
                            }

                            if (info.maxStack < 2)
                            {
                                info.maxStack = 2;
                            }
                        }

                        info.ILCode = ilAddress;
                        info.ILCodeSize = ilLength;
                    }
                }

                CorJitResult result = Compiler.CompileMethod(thisPtr, comp, ref info, flags, out nativeEntry, out nativeSizeOfCode);

                if (ilAddress != IntPtr.Zero)
                    Marshal.FreeHGlobal(ilAddress);

                if(sigAddress != IntPtr.Zero)
                    Marshal.FreeHGlobal(sigAddress);

                //Debug.Assert(result == CorJitResult.CORJIT_OK, "Failed compile");

                //Write bytecode to replace
                if (compileContext?.Mode == CompileContext.ResolveMode.ASM)
                {
                    Marshal.Copy(compileContext.ByteCode, 0, nativeEntry, compileContext.ByteCode.Length);
                }

                return result;
            }
            finally
            {
                compileEntry.EnterCount--;
            }
        }

        public static ManagedJit GetInstance()
        {
            lock (JitLock)
            {
                _instance ??= new ManagedJit();

                if (_instance == null)
                    Debugger.Break();

                return _instance;
            }
        }

        private void ResolveToken(IntPtr thisHandle, ref CORINFO_RESOLVED_TOKEN pResolvedToken)
        {
            _tokenTls ??= new TokenTls();

            if (thisHandle == IntPtr.Zero)
                return;

            _tokenTls.EnterCount++;

            if (_resolversToken == null)
            {
                try
                {
                    _ceeInfo.ResolveToken(thisHandle, ref pResolvedToken);
                }
                finally
                {
                    _tokenTls.EnterCount--;
                }

                return;
            }

            try
            {
                if (_tokenTls.EnterCount == 1)
                {
                    //Capture the method who trying resolve that token.
                    _tokenTls.Source = _tokenTls.GetSource();

                    TokenContext context = new TokenContext(ref pResolvedToken, _tokenTls.Source, _ceeInfo);

                    //TODO
                    //Cascade resolvers
                    foreach (ResolveTokenHandle resolver in _resolversToken.GetInvocationList())
                    {
                        resolver(context);

                        if (context.IsResolved)
                        {
                            pResolvedToken = context.ResolvedToken;
                            break;
                        }
                    }
                }

                _ceeInfo.ResolveToken(thisHandle, ref pResolvedToken);
            }
            finally
            {
                _tokenTls.EnterCount--;
            }
        }

        public void Dispose()
        {
            lock (JitLock)
            {
                if (_isDisposed)
                    return;

                _hookManager.RemoveHook(_resolveToken);
                _hookManager.RemoveHook(_compileMethod);

                _resolversCompile = null;
                _resolversToken = null;

                _compileMethod = null;
                _resolveToken = null;

                _instance = null;
                _isDisposed = true;
            }

            GC.SuppressFinalize(this);
        }

    }
}