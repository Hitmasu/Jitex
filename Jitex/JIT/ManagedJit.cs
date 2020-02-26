using CoreRT.JitInterface;
using Jitex.Utils;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using static CoreRT.JitInterface.CorJitCompiler;
using static Jitex.Utils.Memory;
using static Jitex.Utils.WinApi;
using MethodBody = Jitex.Builder.MethodBody;

namespace Jitex.JIT
{
    /// <summary>
    /// Hook current jit.
    /// </summary>
    /// <remarks>
    /// Source: https://xoofx.com/blog/2018/04/12/writing-managed-jit-in-csharp-with-coreclr/
    /// </remarks>
    public class ManagedJit : IDisposable
    {
        [DllImport("clrjit.dll", CallingConvention = CallingConvention.StdCall, SetLastError = true, EntryPoint = "getJit", BestFitMapping = true)]
        private static extern IntPtr GetJit();

        [ThreadStatic] private static CompileTls _compileTls;

        private static readonly IntPtr JitVTable;
        private static readonly CorJitCompiler Compiler;

        private static readonly object JitLock;
        private static readonly ConcurrentDictionary<IntPtr, Assembly> MapHandleToAssembly;

        private static ManagedJit _instance;
        private static bool _hookInstalled;

        private static IntPtr _corJitInfoPtr = IntPtr.Zero;
        private static CorInfoImpl _corInfoImpl;

        private CompileMethodDelegate _customCompileMethod;
        private IntPtr _customCompiledMethodPtr;

        private bool _isDisposed;

        public delegate ReplaceInfo PreCompile(MethodBase method);
        
        public PreCompile OnPreCompile { get; set; }

        static ManagedJit()
        {
            JitLock = new object();
            MapHandleToAssembly = new ConcurrentDictionary<IntPtr, Assembly>(IntPtrEqualityComparer.Instance);

            IntPtr jit = GetJit();

            JitVTable = Marshal.ReadIntPtr(jit);
            Compiler = Marshal.PtrToStructure<CorJitCompiler>(JitVTable);
        }

        /// <summary>
        /// Prepare custom JIT.
        /// </summary>
        private ManagedJit()
        {
            if (Compiler.CompileMethod == null) return;

            _customCompileMethod = CompileMethod;
            _customCompiledMethodPtr = Marshal.GetFunctionPointerForDelegate(_customCompileMethod);

            IntPtr trampolinePtr = AllocateTrampoline(_customCompiledMethodPtr);
            CompileMethodDelegate trampoline = Marshal.GetDelegateForFunctionPointer<CompileMethodDelegate>(trampolinePtr);

            CORINFO_METHOD_INFO emptyInfo = default;
            trampoline(IntPtr.Zero, IntPtr.Zero, ref emptyInfo, 0, out _, out _);

            FreeTrampoline(trampolinePtr);

            InstallCompileMethod(Marshal.GetFunctionPointerForDelegate(_customCompileMethod));
            _hookInstalled = true;
        }

        public static ManagedJit GetInstance()
        {
            lock (JitLock)
            {
                return _instance ??= new ManagedJit();
            }
        }

        /// <summary>
        /// Set address of CompileMethod in VTable.
        /// </summary>
        /// <param name="compileMethodPtr">The address of method.</param>
        private static void InstallCompileMethod(IntPtr compileMethodPtr)
        {
            VirtualProtect(JitVTable, new IntPtr(IntPtr.Size), MemoryProtection.ReadWrite, out var oldFlags);
            Marshal.WriteIntPtr(JitVTable, compileMethodPtr);
            VirtualProtect(JitVTable, new IntPtr(IntPtr.Size), oldFlags, out _);
        }

        /// <summary>
        /// Wrap delegate to compileMethod from ICorJitCompiler.
        /// </summary>
        /// <param name="thisPtr">this parameter.</param>
        /// <param name="comp">(IN) - Pointer to ICorJitInfo.</param>
        /// <param name="info">(IN) - Pointer to CORINFO_METHOD_INFO.</param>
        /// <param name="flags">(IN) - Pointer to CorJitFlag.</param>
        /// <param name="nativeEntry">(OUT) - Pointer to NativeEntry.</param>
        /// <param name="nativeSizeOfCode">(OUT) - Size of NativeEntry.</param>
        private int CompileMethod(IntPtr thisPtr, IntPtr comp, ref CORINFO_METHOD_INFO info, uint flags, out IntPtr nativeEntry, out int nativeSizeOfCode)
        {
            CompileTls compileEntry = _compileTls ??= new CompileTls();
            compileEntry.EnterCount++;

            try
            {
                if (!_hookInstalled)
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
                            _corInfoImpl = Marshal.PtrToStructure<CorInfoImpl>(_corJitInfoPtr);
                        }

                        IntPtr assemblyHandle = _corInfoImpl.GetModuleAssembly(_corJitInfoPtr, info.scope);

                        if (!MapHandleToAssembly.TryGetValue(assemblyHandle, out Assembly assemblyFound))
                        {
                            IntPtr assemblyNamePtr = _corInfoImpl.GetAssemblyName(_corJitInfoPtr, assemblyHandle);
                            string assemblyName = Marshal.PtrToStringAnsi(assemblyNamePtr);
                            assemblyFound = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(assembly => assembly.GetName().Name == assemblyName);
                            MapHandleToAssembly.TryAdd(assemblyHandle, assemblyFound);
                        }

                        if (assemblyFound != null)
                        {
                            uint methodToken = _corInfoImpl.GetMethodDefFromMethod(_corJitInfoPtr, info.ftn);

                            foreach (Module module in assemblyFound.Modules)
                            {
                                try
                                {
                                    var methodFound = module.ResolveMethod((int)methodToken);
                                    replaceInfo = OnPreCompile(methodFound);
                                }
                                catch
                                {
                                    // ignored
                                }
                            }
                        }
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
                                    info.locals.pSig = sig + 1;
                                    info.locals.args = sig + 3;
                                }

                                info.locals.numArgs = (ushort)methodBody.LocalVariables.Count;
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
                            //the minimal IL is 4 byte = JIT will compile to 21 bytes (byte-code)
                            //Upper that, for each bitwise operation (2 Bytes - ldc.i4 + And) is generated 3 byte-code
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

                int result = Compiler.CompileMethod(thisPtr, comp, ref info, flags, out nativeEntry, out nativeSizeOfCode);

                //ASM can be replaced just after method already compiled by jit.
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
                if (_isDisposed) return;

                InstallCompileMethod(Marshal.GetFunctionPointerForDelegate(Compiler.CompileMethod));
                _customCompileMethod = null;
                _customCompiledMethodPtr = IntPtr.Zero;
                _instance = null;
                _hookInstalled = false;
                _isDisposed = true;
            }

            GC.SuppressFinalize(this);
        }
    }
}
