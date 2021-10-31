using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Jitex.Exceptions;
using Jitex.Framework;
using Jitex.PE;
using Jitex.Runtime;
using Jitex.Utils.Extension;

namespace Jitex.Utils
{
    public static class MethodHelper
    {
        private static readonly bool CanRecompileMethod;

        private static readonly Type CanonType;
        private static readonly IntPtr PrecodeFixupThunkAddress;
        private static readonly ConstructorInfo CtorRuntimeMethodHandeInternal;
        private static readonly MethodInfo GetMethodBase;
        private static readonly MethodInfo GetMethodDescriptorInfo;
        private static readonly MethodInfo GetFunctionPointerInternal;
        private static readonly MethodInfo GetSlot;

        static MethodHelper()
        {
            Type runtimeMethodHandleInternalType = Type.GetType("System.RuntimeMethodHandleInternal")!;
            Type runtimeType = Type.GetType("System.RuntimeType")!;
            Type iRuntimeMethodInfo = Type.GetType("System.IRuntimeMethodInfo")!;

            CanonType = Type.GetType("System.__Canon")!;

            CtorRuntimeMethodHandeInternal = runtimeMethodHandleInternalType.GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { typeof(IntPtr) }, null)!;

            GetMethodBase = runtimeType.GetMethod("GetMethodBase", BindingFlags.NonPublic | BindingFlags.Static, null, new[] { runtimeType, runtimeMethodHandleInternalType }, null)!;

            GetMethodDescriptorInfo = typeof(DynamicMethod).GetMethod("GetMethodDescriptor", BindingFlags.Instance | BindingFlags.NonPublic)!;

            GetFunctionPointerInternal = typeof(RuntimeMethodHandle).GetMethod("GetFunctionPointer", BindingFlags.Static | BindingFlags.NonPublic)!;

            GetSlot = typeof(RuntimeMethodHandle).GetMethod("GetSlot", BindingFlags.Static | BindingFlags.NonPublic, null, new[] { iRuntimeMethodInfo }, null)!;

            PrecodeFixupThunkAddress = GetPrecodeFixupThunkAddress();

            CanRecompileMethod = RuntimeFramework.Framework >= new Version(5, 0, 0);
        }

        private static IntPtr GetPrecodeFixupThunkAddress()
        {
            MethodInfo methodStub = typeof(MethodHelper).GetMethod("MethodToNeverBeCalled", BindingFlags.Static | BindingFlags.NonPublic)!;
            IntPtr functionPointer = methodStub.MethodHandle.GetFunctionPointer();
            int jmpSize = Marshal.ReadInt32(functionPointer, 1);

            return functionPointer + jmpSize + 5;
        }

        private static object? GetRuntimeMethodHandleInternal(IntPtr methodHandle)
        {
            return CtorRuntimeMethodHandeInternal!.Invoke(new object?[] { methodHandle });
        }

        private static int GetMethodSlot(MethodInfo method)
        {
            return (int)GetSlot.Invoke(null, new object[] { method });
        }

        internal static MethodBase GetBaseMethodGeneric(MethodBase method)
        {
            MethodBase originalMethod = method;
            bool methodHasCanon = HasCanon(method, false);
            bool typeHasCanon = TypeHelper.HasCanon(method.DeclaringType);

            if (methodHasCanon || (typeHasCanon && (method.IsGenericMethod || method.IsStatic)))
            {
                IntPtr methodHandle = GetDirectMethodHandle(method);
                originalMethod = GetMethodFromHandle(methodHandle)!;
            }
            else if (typeHasCanon)
            {
                Type typeCanon = TypeHelper.GetBaseTypeGeneric(method.DeclaringType!);
                originalMethod = MethodBase.GetMethodFromHandle(method.MethodHandle, typeCanon.TypeHandle);
            }

            return originalMethod;
        }

        public static MethodBase GetOriginalMethod(MethodBase method)
        {
            return GetBaseMethodGeneric(method);
        }

        internal static bool IsGenericInitialized(MethodBase method)
        {
            if (method.DeclaringType is { IsGenericType: true })
            {
                foreach (Type type in method.DeclaringType.GetGenericArguments())
                {
                    if (type.IsGenericParameter)
                        return false;
                }
            }

            if (method.IsGenericMethod)
            {
                MethodInfo methodInfo = (MethodInfo)method;

                foreach (Type type in methodInfo.GetGenericArguments())
                {
                    if (type.IsGenericParameter)
                        return false;
                }
            }

            return true;
        }

        internal static bool HasCanon(MethodBase method, bool checkDeclaredType = true, bool ignoreCanonType = false)
        {
            bool hasCanon = false;

            if (method is MethodInfo { IsGenericMethod: true } methodInfo)
            {
                Type[] types = method.GetGenericArguments();

                foreach (Type type in types)
                {
                    if (ignoreCanonType && type == CanonType)
                        continue;

                    if (type.IsCanon())
                    {
                        hasCanon = true;
                        break;
                    }
                }
            }

            if (!hasCanon && checkDeclaredType && method.DeclaringType != null)
                hasCanon = TypeHelper.HasCanon(method.DeclaringType);

            return hasCanon;
        }

        /// <summary>
        /// Get handle from a method.
        /// </summary>
        /// <param name="method">Method to get handle.</param>
        /// <returns>Handle from method.</returns>
        public static RuntimeMethodHandle GetMethodHandle(MethodBase method)
        {
            if (method == null) throw new ArgumentNullException(nameof(method));

            if (method is DynamicMethod)
                return (RuntimeMethodHandle)GetMethodDescriptorInfo.Invoke(method, null);

            return method.MethodHandle;
        }

        public static IntPtr GetFunctionPointer(IntPtr methodHandle)
        {
            object handle = GetRuntimeMethodHandleInternal(methodHandle);
            return (IntPtr)GetFunctionPointerInternal.Invoke(null, new[] { handle });
        }
        private static IntPtr GetDirectMethodHandle(MethodBase method)
        {
            bool methodHasCanon = HasCanon(method, false, true);
            bool typeHasCanon = TypeHelper.HasCanon(method.DeclaringType, true);

            IntPtr methodHandle = method.MethodHandle.Value;

            if (CanRecompileMethod && (methodHasCanon || typeHasCanon && (method.IsGenericMethod || method.IsStatic)))
                methodHandle = Marshal.ReadIntPtr(methodHandle, IntPtr.Size);
            else
                methodHandle = GetMethodHandle(method).Value;

            return methodHandle;
        }

        /// <summary>
        /// Get method from a handle.
        /// </summary>
        /// <param name="methodHandle">Handle of method.</param>
        /// <returns>Method from handle.</returns>
        public static MethodBase? GetMethodFromHandle(IntPtr methodHandle)
        {
            object? handle = GetRuntimeMethodHandleInternal(methodHandle);
            MethodBase? method = GetMethodBase.Invoke(null, new[] { null, handle }) as MethodBase;
            return method;
        }

        /// <summary>
        /// Get method from handle and type.
        /// </summary>
        /// <param name="methodHandle">Handle of method.</param>
        /// <param name="typeHandle">Handle of type.</param>
        /// <returns>Method from handle and type.</returns>
        public static MethodBase? GetMethodFromHandle(IntPtr methodHandle, IntPtr typeHandle)
        {
            MethodBase? method = GetMethodFromHandle(methodHandle);

            if (method == null)
                return null;

            Type type = TypeHelper.GetTypeFromHandle(typeHandle);

            RuntimeMethodHandle handle = GetMethodHandle(method);
            return MethodBase.GetMethodFromHandle(handle, type.TypeHandle);
        }

        public static IntPtr GetNativeAddress(MethodBase method)
        {
            RuntimeMethodHandle handle = GetMethodHandle(method);
            RuntimeHelpers.PrepareMethod(handle);

            IntPtr functionPointer = handle.GetFunctionPointer();

            byte opCode = MemoryHelper.Read<byte>(functionPointer, 0);

            if (opCode == 0xE9)
            {
                int jmpSize = MemoryHelper.Read<int>(functionPointer, 1);
                return functionPointer + jmpSize + 5;
            }

            return functionPointer;
        }

        /// <summary>
        /// Returns if method was compiled as ReadyToRun (R2R).
        /// </summary>
        /// <param name="method">Method to check ReadyToRun.</param>
        /// <returns>Returns true is was compiled as ReadyToRun otherwise false.</returns>
        public static bool IsReadyToRun(MethodBase method)
        {
            NativeReader reader = new NativeReader(method.Module);
            return reader.IsReadyToRun(method);
        }

        /// <summary>
        /// Disable ReadyToRun on method, forcing method to be compiled by jit.
        /// </summary>
        /// <param name="method">Method to disable ReadyToRun.</param>
        /// <returns>Returns false if method is not ReadyToRun otherwise true.</returns>
        public static bool DisableReadyToRun(MethodBase method)
        {
            if (!IsReadyToRun(method))
                return false;

            NativeReader reader = new NativeReader(method.Module);
            return reader.DisableReadyToRun(method);
        }

        /// <summary>
        /// Returns if method is already compiled.
        /// </summary>
        /// <param name="method">Method to get status.</param>
        /// <returns>Returns true if method is compiled, otherwise returns false.</returns>
        public static bool IsCompiled(MethodBase method)
        {
            if (method == null) throw new ArgumentNullException(nameof(method));

            method = GetOriginalMethod(method);
            IntPtr methodHandle = GetDirectMethodHandle(method);
            int offset = GetFunctionPointerOffset(method);
            return Marshal.ReadIntPtr(methodHandle, IntPtr.Size * offset) != IntPtr.Zero;
        }

        /// <summary>
        /// Return is method can be resolved
        /// </summary>
        /// <param name="method">Method to check</param>
        /// <returns>Returns true if method is resolvable, otherwise returns false.</returns>
        public static bool IsResolvable(MethodBase method)
        {
            if (method == null) throw new ArgumentNullException(nameof(method));

            CheckIfGenericIsInitialized(method);

            method = GetOriginalMethod(method);

            if (!IsCompiled(method))
                return true;

            return RuntimeMethodCache.GetMethodCompiledInfo(method) != null;
        }

        /// <summary>
        /// Set state method to be compiled
        /// </summary>
        /// <param name="method">Method to be compiled.</param>
        /// <returns>Returns if state was set sucessfully.</returns>
        public static bool ForceRecompile(MethodBase method)
        {
            if (!CanRecompileMethod) throw new UnsupportedFrameworkVersion("Recompile method is only supported on .NET 5 or above.");
            if (method == null) throw new ArgumentNullException(nameof(method));

            CheckIfGenericIsInitialized(method);

            return SetMethodPreCode(method);
        }

        internal static bool SetMethodPreCode(MethodBase method)
        {
            IntPtr methodHandle = GetDirectMethodHandle(method);
            IntPtr functionPointer = GetFunctionPointer(methodHandle);
            int jmpSize = (int)(PrecodeFixupThunkAddress.ToInt64() - functionPointer.ToInt64() - 5);
            int offset = GetFunctionPointerOffset(method);

            //Remove funcitonPointer on MethodDesc
            Marshal.WriteIntPtr(methodHandle, IntPtr.Size * offset, IntPtr.Zero);

            //Set call instruction to call PreCodeFixup
            Marshal.WriteByte(functionPointer, 0xE8); //call instruction
            Marshal.WriteByte(functionPointer, 5, 0x5E); //pop instruction
            Marshal.WriteInt32(functionPointer, 1, jmpSize);

            if (!method.IsVirtual)
                return false;

            IntPtr typeHandle = method.DeclaringType!.TypeHandle.Value;

            //To find start of vtable, currently we need search by some specific value on TypeDesc.
            //That's a dumb way to get address of vtable, but sadly, i can't find a better way.
            //TODO: Find a better way to get vtable from type.
            IntPtr startVTable = typeHandle + IntPtr.Size * 2;
            IntPtr endVTable = startVTable + IntPtr.Size * 100;

            Type? lastInterface = method.DeclaringType.GetInterfaces().LastOrDefault();

            bool addressFound = false;

            if (lastInterface != null)
            {
                //When classes implements interfaces, we can find the start of vtable searching by TypeHandle from interfaces implemented.
                //All TypeHandles from all interfaces, will stored on TypeDesc. 
                //VTable starts after last TypeHandle from last interface implemented.
                //
                //Eg.:
                // ------
                // public class MyType : IInterface1, IInterface2, IInterface3
                // ------
                //<address>:    (Number of Interfaces + 1) 00 00 00 00 00 00 00
                //<address+8>:  IInterface1TypeHandle
                //<address+16>: IInterface2TypeHandle
                //<address+24>: IInterface3TypeHandle
                //<address+32>: VTable

                do
                {
                    if (MemoryHelper.Read<IntPtr>(startVTable) == lastInterface.TypeHandle.Value)
                    {
                        startVTable += IntPtr.Size;
                        addressFound = true;
                        break;
                    }

                    startVTable += IntPtr.Size;

                } while (startVTable.ToInt64() < endVTable.ToInt64());
            }
            else
            {
                //Normal classes have a weird pointer to vtable:
                //---
                //<address>:    00 00 00 00 00 00 00 00
                //<address+8>:  [pointer to <address+16>]
                //<address+16>: VTable
                //---
                //So, we basically need find an address value which pointer to the next address.
                do
                {
                    IntPtr value = MemoryHelper.Read<IntPtr>(startVTable);
                    startVTable += IntPtr.Size;

                    if (value == startVTable)
                    {
                        addressFound = true;
                        break;
                    }

                } while (startVTable.ToInt64() < endVTable.ToInt64());
            }

            if (!addressFound)
                return false;

            int originalSlot = GetMethodSlot((MethodInfo)method);
            int vTableIndex = 0; //+1 is for ctor.

            //If method is virtual, we need get the "virtual" function pointer, which sadly, it's not same from MethodHandle.
            //I can't find a way to get that pointer, but we can assume his allocated after/before function pointer from last constructor:
            //Eg.:
            //--
            //<address>:    Constructor[0] Function Pointer
            //<address+8>:  Constructor[1] Function Pointer
            //<address+16>: Constructor[2] Function Pointer
            //....
            //<address+..>: Virtual Methods Function Pointer
            //--

            IOrderedEnumerable<MethodInfo> methods = method.DeclaringType.GetMethods((BindingFlags)(-1))
                .OrderBy(w => w.MetadataToken);

            foreach (MethodInfo methodInfo in methods)
            {
                if (methodInfo.MetadataToken == method.MetadataToken)
                    break;

                if (IsMethodInVtable(methodInfo))
                    vTableIndex++;
            }

            ConstructorInfo lastCtor = method.DeclaringType!.GetConstructors((BindingFlags)(-1)).Last();

            if (lastCtor.MetadataToken > method.MetadataToken)
            {
                vTableIndex--;

                if (vTableIndex > 0)
                    vTableIndex = -vTableIndex;
            }

            IntPtr ctorPointer = lastCtor.MethodHandle.GetFunctionPointer();
            IntPtr virtualFunctionPointer = ctorPointer + IntPtr.Size * vTableIndex;

            if (virtualFunctionPointer == ctorPointer)
            {
                if (lastCtor.MetadataToken > method.MetadataToken)
                    virtualFunctionPointer -= IntPtr.Size;
                else
                    virtualFunctionPointer += IntPtr.Size;
            }

            MemoryHelper.Write(startVTable, IntPtr.Size * originalSlot, virtualFunctionPointer);

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetFunctionPointerOffset(MethodBase method)
        {
            if ((TypeHelper.IsGeneric(method.DeclaringType) && !method.IsGenericMethod) || method.IsVirtual)
                return 1;

            if (method.IsGenericMethod)
                return 5;

            return 2;
        }

        internal static void PrepareMethod(MethodBase method)
        {
            RuntimeMethodHandle handle = GetMethodHandle(method);
            RuntimeHelpers.PrepareMethod(handle);
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        [Obsolete("This method shouldn't be called!")]
        private static void MethodToNeverBeCalled()
        {
            throw new NotImplementedException("This method shouldn't be called!");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CheckIfGenericIsInitialized(MethodBase method)
        {
            if (!IsGenericInitialized(method))
            {
                MethodInfo methodInfo = (MethodInfo)method;

                if (method == methodInfo.GetGenericMethodDefinition())
                    throw new ArgumentException("Generic methods cannot be recompiled by generic method definition.\n" +
                                                "It's necessary substitute generic parameters types from generic definition: "
                                                + method);
            }
        }

        /// <summary>
        /// Get RID from a method.
        /// </summary>
        /// <param name="method">Method to get RID.</param>
        /// <returns>RID from method.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetRID(MethodBase method) => method.MetadataToken & 0x00FFFFFF;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsMethodInVtable(MethodBase method) => method.IsVirtual || method.IsGenericMethod;
    }
}