using Jitex.Hook;
using Jitex.JIT.CorInfo;
using Jitex.Utils;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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
        private CompileMethodDelegate _compileMethod;

        private bool _isDisposed;

        private ResolveTokenDelegate _resolveToken;

        [ThreadStatic] private static CompileTls _compileTls;

        [ThreadStatic] private static TokenTls _tokenTls;

        private static readonly object JitLock;

        private static ManagedJit _instance;

        private static bool _hookCompilerInstalled;
        private static bool _hookTokenInstalled;

        private static readonly IntPtr JitVTable;
        private static readonly CorJitCompiler Compiler;

        private static IntPtr _corJitInfoPtr = IntPtr.Zero;
        private static CEEInfo _ceeInfo;

        public PreCompileHandle OnPreCompile { get; set; }

        public ResolveTokenHandle OnResolveToken { get; set; }

        public delegate ReplaceInfo PreCompileHandle(MethodBase method);

        public delegate void ResolveTokenHandle(TokenContext token);

        static ManagedJit()
        {
            JitLock = new object();

            IntPtr jit = GetJit();

            JitVTable = Marshal.ReadIntPtr(jit);
            Compiler = Marshal.PtrToStructure<CorJitCompiler>(JitVTable);
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
            RuntimeHelperExtension.PrepareDelegate(_compileMethod, IntPtr.Zero, IntPtr.Zero, emptyInfo, (uint) 0, IntPtr.Zero, 0);
            RuntimeHelpers.PrepareDelegate(OnResolveToken);

            _hookManager.InjectHook(JitVTable, _compileMethod);
            _hookCompilerInstalled = true;
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
                if (!_hookCompilerInstalled)
                {
                    nativeEntry = IntPtr.Zero;
                    nativeSizeOfCode = 0;
                    return 0;
                }

                ReplaceInfo replaceInfo = null;

                if (compileEntry.EnterCount == 1 && OnPreCompile != null)
                {
                    lock (JitLock)
                    {
                        if (_corJitInfoPtr == IntPtr.Zero)
                        {
                            _corJitInfoPtr = Marshal.ReadIntPtr(comp);
                            _ceeInfo = new CEEInfo(_corJitInfoPtr);
                            _hookManager.InjectHook(_ceeInfo.ResolveTokenIndex, _resolveToken);
                            _hookTokenInstalled = true;
                        }

                        Module module = AppModules.GetModuleByPointer(info.scope);

                        if (module != null)
                        {
                            uint methodToken = _ceeInfo.GetMethodDefFromMethod(info.ftn);
                            MethodBase methodFound = module.ResolveMethod((int) methodToken);
                            _tokenTls = new TokenTls {Root = methodFound};
                            replaceInfo = OnPreCompile(methodFound);
                        }
                    }

                    if (replaceInfo != null)
                    {
                        int ilLength;
                        IntPtr ilAddress;

                        if (replaceInfo.Mode == ReplaceInfo.ReplaceMode.IL)
                        {
                            MethodBody methodBody = replaceInfo.MethodBody;

                            ilLength = methodBody.IL.Length;

                            unsafe
                            {
                                fixed (void* ptr = methodBody.IL)
                                {
                                    ilAddress = new IntPtr(ptr);
                                }

                                if (methodBody.HasLocalVariable)
                                {
                                    fixed (byte* sig = methodBody.GetSignatureVariables())
                                    {
                                        //pSig starts after length of signature
                                        info.locals.pSig = sig + 1;

                                        //args starts after definition of signature (0x07)
                                        info.locals.args = sig + 3;
                                    }

                                    info.locals.numArgs = (ushort) methodBody.LocalVariables.Count;
                                }
                            }

                            info.maxStack = methodBody.MaxStackSize;
                        }
                        else
                        {
                            Span<byte> emptyBody;

                            //Min size byte-code generated by JIT
                            const int minSize = 21;

                            if (replaceInfo.ByteCode.Length > minSize)
                            {
                                //Calculate the size of IL to allocate byte-code
                                //Minimal size IL is 4 byte = JIT will compile to 21 bytes (byte-code)
                                //Upper that, for each bitwise operation (ldc.i4 + And) is generated 3 byte-code
                                //Example: 
                                //IL with 1 bitwise = 21 bytes
                                //IL with 2 bitwise = 24 bytes
                                //IL with 3 bitwise = 27 bytes
                                //...
                                int nextMax = replaceInfo.ByteCode.Length + (3 - replaceInfo.ByteCode.Length % 3);
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
                            emptyBody[0] = (byte) OpCodes.Ldc_I4_1.Value;
                            emptyBody[1] = (byte) OpCodes.Ldc_I4_1.Value;
                            emptyBody[2] = (byte) OpCodes.And.Value;
                            emptyBody[^1] = (byte) OpCodes.Ret.Value;

                            for (int i = 3; i < emptyBody.Length - 2; i += 2)
                            {
                                emptyBody[i] = (byte) OpCodes.Ldc_I4_1.Value;
                                emptyBody[i + 1] = (byte) OpCodes.And.Value;
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

                Debug.Assert(result == CorJitResult.CORJIT_OK, "Failed compile");

                //Write bytecode to replace
                if (replaceInfo?.Mode == ReplaceInfo.ReplaceMode.ASM)
                {
                    Marshal.Copy(replaceInfo.ByteCode, 0, nativeEntry, replaceInfo.ByteCode.Length);
                }

                return result;
            }
            finally
            {
                compileEntry.EnterCount--;
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
                _compileMethod = null;
                _resolveToken = null;
                _instance = null;
                _hookCompilerInstalled = false;
                _isDisposed = true;
            }

            GC.SuppressFinalize(this);
        }

        public static ManagedJit GetInstance()
        {
            lock (JitLock)
            {
                return _instance ??= new ManagedJit();
            }
        }

        private void ResolveToken(IntPtr thisHandle, ref CORINFO_RESOLVED_TOKEN pResolvedToken)
        {
            _tokenTls ??= new TokenTls();

            if (!_hookTokenInstalled)
                return;

            _tokenTls.EnterCount++;

            if (OnResolveToken == null)
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
                    OnResolveToken(context);

                    pResolvedToken = context.ResolvedToken;
                }
            }
            finally
            {
                _ceeInfo.ResolveToken(thisHandle, ref pResolvedToken);
                _tokenTls.EnterCount--;
            }
        }
    }
}