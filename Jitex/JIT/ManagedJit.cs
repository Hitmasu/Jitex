using Jitex.JIT.CORTypes;
using Jitex.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Jitex.JIT.PInvoke;
using static Jitex.JIT.CORTypes.Delegates;
using static Jitex.JIT.PInvoke.Structs;
using static Jitex.Tools.Memory;
using static Jitex.Tools.WinApi;

namespace Jitex.JIT
{
    /// <summary>
    /// Hook current managed jit.
    /// </summary>
    /// <remarks>
    /// Source: https://xoofx.com/blog/2018/04/12/writing-managed-jit-in-csharp-with-coreclr/
    /// </remarks>
    public class ManagedJit
    {
        [ThreadStatic] private static CompileTls _compileTls;

        private static readonly IntPtr JitVTable;
        private static readonly CompileMethodDelegate DefaultCompileMethod;
        private static readonly object JitLock;
        private static readonly Dictionary<IntPtr, Assembly> MapHandleToAssembly;

        private static ManagedJit _instance;
        private static bool _hookInstalled;

        private static IntPtr _corJitInfoPtr;

        public delegate ReplaceInfo PreCompile(MethodBase method);

        public PreCompile OnPreCompile { get; set; }

        static ManagedJit()
        {
            JitLock = new object();
            MapHandleToAssembly = new Dictionary<IntPtr, Assembly>(IntPtrEqualityComparer.Instance);

            if (!NativeLibrary.TryLoad("clrjit", out var jitAddress))
                return;

            if (!NativeLibrary.TryGetExport(jitAddress, "getJit", out var getJitAddress))
                return;

            GetJitDelegate getJit = (GetJitDelegate) Marshal.GetDelegateForFunctionPointer(getJitAddress, typeof(GetJitDelegate));
            IntPtr jit = getJit();

            JitVTable = Marshal.ReadIntPtr(jit);

            IntPtr defaultCompileMethodPtr = Marshal.ReadIntPtr(JitVTable, IntPtr.Size * VTable.CompileMethod);
            DefaultCompileMethod = Marshal.GetDelegateForFunctionPointer<CompileMethodDelegate>(defaultCompileMethodPtr);
        }

        private ManagedJit()
        {
            if (DefaultCompileMethod == null) return;

            CompileMethodDelegate compileMethodDelegate = CompileMethod;
            IntPtr compileMethodPtr = Marshal.GetFunctionPointerForDelegate(compileMethodDelegate);

            IntPtr trampolinePtr = AllocateTrampoline(compileMethodPtr);
            CompileMethodDelegate trampoline = Marshal.GetDelegateForFunctionPointer<CompileMethodDelegate>(trampolinePtr);

            CORINFO_METHOD_INFO emptyInfo = default;

            trampoline(IntPtr.Zero, IntPtr.Zero, ref emptyInfo, 0, out _, out int _);

            FreeTrampoline(trampolinePtr);
            InstallManagedJit(compileMethodPtr);
            _hookInstalled = true;
        }

        public static ManagedJit GetInstance()
        {
            lock (JitLock)
            {
                return _instance ??= new ManagedJit();
            }
        }

        private static void InstallManagedJit(IntPtr compileMethodPtr)
        {
            VirtualProtect(JitVTable + VTable.CompileMethod, new IntPtr(IntPtr.Size), MemoryProtection.ReadWrite, out var oldFlags);
            Marshal.WriteIntPtr(JitVTable, VTable.CompileMethod, compileMethodPtr);
            VirtualProtect(JitVTable + VTable.CompileMethod, new IntPtr(IntPtr.Size), oldFlags, out _);
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
                    if (_corJitInfoPtr == default)
                    {
                        _corJitInfoPtr = Marshal.ReadIntPtr(comp);
                    }

                    lock (JitLock)
                    {
                        IntPtr assemblyHandle = ExecuteCEEInfo<GetModuleAssemblyDelegate, IntPtr, IntPtr>(info.scope, VTable.GetModuleAssembly);

                        if (!MapHandleToAssembly.TryGetValue(assemblyHandle, out Assembly assemblyFound))
                        {
                            IntPtr assemblyNamePtr = ExecuteCEEInfo<GetAssemblyName, IntPtr, IntPtr>(assemblyHandle, VTable.GetAssemblyName);
                            string assemblyName = Marshal.PtrToStringAnsi(assemblyNamePtr);
                            assemblyFound = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(assembly => assembly.GetName().Name == assemblyName);
                            MapHandleToAssembly.Add(assemblyHandle, assemblyFound);
                        }

                        if (assemblyFound != null)
                        {
                            int methodToken = ExecuteCEEInfo<GetMethodDefFromMethodDelegate, int, IntPtr>(info.ftn, VTable.GetMethodDefFromMethod);

                            foreach (Module module in assemblyFound.Modules)
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
                    IntPtr newILAddress = Marshal.AllocHGlobal(replaceInfo.Body.Length);

                    if (replaceInfo.Mode == ReplaceInfo.ReplaceMode.IL)
                    {
                        Marshal.Copy(replaceInfo.Body, 0, newILAddress, replaceInfo.Body.Length);
                    }
                    else
                    {
                        //Create a empty body to inject asm:
                        //{
                        //  return;
                        //}
                        byte[] emptyBody;

                        //After compile, JIT will allocate 34 bytes to our empty body,
                        //if we need more space (large asm instrucions), we should allocate more space to our IL using br.s.
                        if (replaceInfo.Body.Length > 34 && replaceInfo.Body.Length % 2 != 0)
                        {
                            emptyBody = new byte[replaceInfo.Body.Length + 1];
                        }
                        else
                        {
                            emptyBody = new byte[replaceInfo.Body.Length];
                        }

                        emptyBody[0] = 0x2b; //br.s
                        emptyBody[^1] = 0x2a; //ret


                        if (info.args.retType != Enums.CorInfoType.CORINFO_TYPE_VOID)
                        {
                            //that is like 'return default'
                            emptyBody[^2] = 0x06; //ldloc.0

                            if (emptyBody.Length > 34)
                            {
                                for (int i = 2; i < emptyBody.Length - 2; i++)
                                {
                                    if (i % 2 == 0)
                                    {
                                        emptyBody[i] = 0x2b;
                                    }
                                }
                            }
                        }


                        Marshal.Copy(emptyBody, 0, newILAddress, emptyBody.Length);
                    }

                    info.ILCode = newILAddress;
                    info.ILCodeSize = replaceInfo.Body.Length;
                }

                int result = DefaultCompileMethod(thisPtr, comp, ref info, flags, out nativeEntry, out nativeSizeOfCode);

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
    }
}