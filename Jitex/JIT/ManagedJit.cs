using Jitex.JIT.CORTypes;
using Jitex.Tools;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using static Jitex.JIT.CORTypes.Delegates;
using static Jitex.JIT.CORTypes.Structs;
using static Jitex.Tools.Memory;
using static Jitex.Tools.WinApi;
using VTable = Jitex.JIT.CORTypes.VTable;

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
        public static extern IntPtr GetJit();

        [ThreadStatic] private static CompileTls _compileTls;

        private static readonly IntPtr JitVTable;
        private static readonly CorJitCompiler compiler;

        private static IsJitOptimizationDisabled _isJitOptimizationDisabledOriginal;
        private static IsJitOptimizationDisabled _customIsJitOptimizationDisabled;

        private static readonly object JitLock;
        private static readonly ConcurrentDictionary<IntPtr, AssemblyInfo> MapHandleToAssembly;

        private static ManagedJit _instance;
        private static bool _hookInstalled;
        private static IntPtr _corJitInfoPtr = IntPtr.Zero;

        private CompileMethodDelegate _customCompileMethod;
        private IntPtr _customCompiledMethodPtr;

        private bool _isDisposed;

        public delegate ReplaceInfo PreCompile(MethodBase method);


        public PreCompile OnPreCompile { get; set; }

        static ManagedJit()
        {
            JitLock = new object();
            MapHandleToAssembly = new ConcurrentDictionary<IntPtr, AssemblyInfo>(IntPtrEqualityComparer.Instance);

            //Obtém o endereço do JIT
            IntPtr jit = GetJit();

            //Obtém a VTable
            JitVTable = Marshal.ReadIntPtr(jit);
            compiler = Marshal.PtrToStructure<CorJitCompiler>(JitVTable);
        }

        /// <summary>
        /// Prepare custom JIT.
        /// </summary>
        private ManagedJit()
        {
            if (compiler.CompileMethod == null) return;

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

        /// <summary>
        /// Get instance of ManagedJIT.
        /// </summary>
        /// <returns></returns>
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
                AssemblyInfo assemblyInfo = null;

                if (compileEntry.EnterCount == 1 && OnPreCompile != null)
                {
                    if (_corJitInfoPtr == IntPtr.Zero)
                    {
                        _corJitInfoPtr = Marshal.ReadIntPtr(comp);
                    }

                    lock (JitLock)
                    {
                        IntPtr assemblyHandle = ExecuteCEEInfo<GetModuleAssemblyDelegate, IntPtr, IntPtr>(info.scope, VTable.GetModuleAssembly);

                        if (!MapHandleToAssembly.TryGetValue(assemblyHandle, out assemblyInfo))
                        {
                            IntPtr assemblyNamePtr = ExecuteCEEInfo<GetAssemblyName, IntPtr, IntPtr>(assemblyHandle, VTable.GetAssemblyName);
                            string assemblyName = Marshal.PtrToStringAnsi(assemblyNamePtr);
                            Assembly assemblyFound = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(assembly => assembly.GetName().Name == assemblyName);
                            assemblyInfo = new AssemblyInfo(assemblyFound);
                            MapHandleToAssembly.TryAdd(assemblyHandle, assemblyInfo);
                        }

                        if (assemblyInfo.Assembly != null)
                        {
                            int methodToken = ExecuteCEEInfo<GetMethodDefFromMethodDelegate, int, IntPtr>(info.ftn, VTable.GetMethodDefFromMethod);

                            foreach (Module module in assemblyInfo.Assembly.Modules)
                            {
                                try
                                {
                                    MethodBase methodFound = module.ResolveMethod(methodToken);
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
                        ilLength = replaceInfo.Body.Length;
                        unsafe
                        {
                            fixed (void* ptr = replaceInfo.Body)
                            {
                                ilAddress = new IntPtr(ptr);
                            }
                        }
                    }
                    else
                    {
                        //Create a empty body to JIT allocate space
                        //{
                        //  return;
                        //}
                        Span<byte> emptyBody;

                        int minSize = assemblyInfo.IsJitOptmizedEnabled ? 13 : 34;

                        if (replaceInfo.Body.Length > minSize && replaceInfo.Body.Length > 21)
                        {
                            int nextMax = replaceInfo.Body.Length + (3 - replaceInfo.Body.Length % 3);
                            ilLength = 4 + 2 * ((nextMax - 21) / 3);
                        }
                        else
                        {
                            ilLength = 4;
                        }

                        if (info.args.retType != Enums.CorInfoType.CORINFO_TYPE_VOID)
                        {
                            ilLength++;
                        }

                        unsafe
                        {
                            void* ptr = stackalloc byte[ilLength];
                            emptyBody = new Span<byte>(ptr, ilLength);
                            ilAddress = new IntPtr(ptr);
                        }

                        emptyBody[0] = (byte) OpCodes.Ldc_I4_1.Value;
                        emptyBody[1] = (byte) OpCodes.Ldc_I4_1.Value;
                        emptyBody[2] = (byte) OpCodes.And.Value;
                        emptyBody[^1] = (byte) OpCodes.Ret.Value;

                        if (info.args.retType != Enums.CorInfoType.CORINFO_TYPE_VOID)
                        {
                            emptyBody[^2] = Marshal.ReadByte(info.ILCode, info.ILCodeSize-2);
                        }

                        int lastIndex = ilLength % 2 == 0 ? ilLength - 1 : ilLength - 2;

                        for (int i = 3; i < lastIndex; i+=2)
                        {
                            emptyBody[i] = (byte) OpCodes.Ldc_I4_1.Value;
                            emptyBody[i+1] = (byte) OpCodes.And.Value;
                        }

                        int a = 10;
                    }

                    info.maxStack = 2;
                    info.ILCode = ilAddress;
                    info.ILCodeSize = ilLength;
                }

                int result = compiler.CompileMethod(thisPtr, comp, ref info, flags, out nativeEntry, out nativeSizeOfCode);

                if (replaceInfo != null)
                {
                    Trace.WriteLine(nativeSizeOfCode);
                    Debugger.Break();
                }


                //ASM can be replaced just after method already compiled by jit.
                if (replaceInfo?.Mode == ReplaceInfo.ReplaceMode.ASM)
                {
                    Marshal.Copy(replaceInfo.Body, 0, nativeEntry, replaceInfo.Body.Length);
                }

                return result;
            }
            finally
            {
                compileEntry.EnterCount--;
            }
        }

        /// <summary>
        /// Execute function from CEEInfo interface.
        /// </summary>
        /// <typeparam name="TDelegate">Type of delegate to excecute.</typeparam>
        /// <typeparam name="TOut">Type of return from method.</typeparam>
        /// <typeparam name="TValue">Type of parameter value.</typeparam>
        /// <param name="value">Value parameter.</param>
        /// <param name="offset">Offset in <see cref="VTable"/>.</param>
        /// <returns>Return from method delegate.</returns>
        private static TOut ExecuteCEEInfo<TDelegate, TOut, TValue>(TValue value, int offset)
        {
            IntPtr delegatePtr = Marshal.ReadIntPtr(_corJitInfoPtr, IntPtr.Size * offset);
            Delegate delegateMethod = Marshal.GetDelegateForFunctionPointer(delegatePtr, typeof(TDelegate));
            return (TOut) delegateMethod.DynamicInvoke(_corJitInfoPtr, value);
        }

        public void Dispose()
        {
            lock (JitLock)
            {
                if (_isDisposed) return;

                InstallCompileMethod(Marshal.GetFunctionPointerForDelegate(compiler.CompileMethod));
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