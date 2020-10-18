using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Jitex.Exceptions;
using Jitex.Runtime;
using Jitex.Runtime.Offsets;
using Jitex.Utils;

namespace Jitex.JIT.CorInfo
{
    internal static class CEEInfo
    {
        private static IntPtr CEEInfoVTable => RuntimeFramework.GetFramework().CEEInfoVTable;

        private static readonly GetMethodDefFromMethodDelegate _getMethodDefFromMethod;

        private static readonly ConstructStringLiteralDelegate _constructStringLiteral;

        private static readonly ResolveTokenDelegate _resolveToken;

        public static IntPtr ResolveTokenIndex { get; }

        public static IntPtr ConstructStringLiteralIndex { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate uint GetMethodDefFromMethodDelegate(IntPtr thisHandle, IntPtr hMethod);

        [UnmanagedFunctionPointer(default)]
        public delegate void ResolveTokenDelegate(IntPtr thisHandle, IntPtr pResolvedToken);

        [UnmanagedFunctionPointer(default)]
        public delegate InfoAccessType ConstructStringLiteralDelegate(IntPtr thisHandle, IntPtr hModule, int metadataToken, IntPtr ptrString);

        static CEEInfo()
        {
            if (CEEInfoVTable == IntPtr.Zero)
                throw new VTableNotLoaded(nameof(CEEInfo));

            IntPtr getMethodDefFromMethodIndex = CEEInfoVTable + IntPtr.Size * CEEInfoOffset.GetMethodDefFromMethod;

            ResolveTokenIndex = CEEInfoVTable + IntPtr.Size * CEEInfoOffset.ResolveToken;
            ConstructStringLiteralIndex = CEEInfoVTable + IntPtr.Size * CEEInfoOffset.ConstructStringLiteral;

            IntPtr resolveTokenPtr = Marshal.ReadIntPtr(ResolveTokenIndex);
            IntPtr getMethodDefFromMethodPtr = Marshal.ReadIntPtr(getMethodDefFromMethodIndex);
            IntPtr constructStringLiteralPtr = Marshal.ReadIntPtr(ConstructStringLiteralIndex);


            _getMethodDefFromMethod = Marshal.GetDelegateForFunctionPointer<GetMethodDefFromMethodDelegate>(getMethodDefFromMethodPtr);
            _resolveToken = Marshal.GetDelegateForFunctionPointer<ResolveTokenDelegate>(resolveTokenPtr);
            //_constructStringLiteral = Marshal.GetDelegateForFunctionPointer<ConstructStringLiteralDelegate>(constructStringLiteralPtr);

            //int sizeId = 10;

            ////byte[] resolveTokenB = new byte[sizeId];
            ////byte[] methodDefB = new byte[sizeId];
            ////byte[] stringB = new byte[sizeId];

            ////Marshal.Copy(resolveTokenPtr, resolveTokenB, 0, sizeId);
            ////Marshal.Copy(getMethodDefFromMethodPtr, methodDefB, 0, sizeId);
            ////Marshal.Copy(constructStringLiteralPtr, stringB, 0, sizeId);

            ////Console.WriteLine(string.Join(", 0x", resolveTokenB.Select(x => x.ToString("X"))));
            ////Console.WriteLine(string.Join(", 0x", methodDefB.Select(x => x.ToString("X"))));
            ////Console.WriteLine(string.Join(", 0x", stringB.Select(x => x.ToString("X"))));

            //byte[] rt = { 0x48, 0x89, 0x4C, 0x24, 0x8, 0x53, 0x56, 0x57, 0x41, 0x54, 0x41, 0x55, 0x41, 0x56, 0x41, 0x57, 0x48, 0x81, 0xEC, 0x30 };
            //byte[] md = { 0x8A, 0x42, 0x6, 0x24, 0x7, 0x3C, 0x7, 0x75, 0x6, 0xB8, 0x0, 0x0, 0x0, 0x6, 0xC3, 0xF, 0xB6, 0x42, 0x2, 0x48 };
            //byte[] st = { 0x4C, 0x8B, 0xDC, 0x57, 0x48, 0x83, 0xEC, 0x40, 0x49, 0xC7, 0x43, 0xE8, 0xFE, 0xFF, 0xFF, 0xFF, 0x49, 0x89, 0x5B, 0x10 };

            //rt = rt.ToList().Take(10).ToArray();
            //md = md.ToList().Take(10).ToArray();
            //st = st.ToList().Take(5).ToArray();
            //int lastIndex = 0;

            //IntPtr destAddress = ConstructStringLiteralIndex;

            //for (int i = 0; ; i++)
            //{
            //    unsafe
            //    {
            //        IntPtr addr = Marshal.ReadIntPtr(ICorJitInfoVTable + IntPtr.Size * i);
            //        Span<byte> bytes = new Span<byte>(addr.ToPointer(), sizeId);
            //        if (bytes.SequenceEqual(rt))
            //        {
            //            Console.WriteLine("Found ResolveTokenOffset: " + i.ToString("X"));
            //            lastIndex = i;
            //            break;
            //        }
            //    }
            //}

            //for (int i = lastIndex; ; i++)
            //{
            //    unsafe
            //    {
            //        IntPtr addr = Marshal.ReadIntPtr(ICorJitInfoVTable + IntPtr.Size * i);
            //        Span<byte> bytes = new Span<byte>(addr.ToPointer(), sizeId);
            //        if (bytes.SequenceEqual(md))
            //        {
            //            Console.WriteLine("Found GetMethodDefFromMethodOffset: " + i.ToString("X"));
            //            lastIndex = i;
            //            break;
            //        }
            //    }
            //}

            //for (int i = 0; ; i++)
            //{
            //    unsafe
            //    {
            //        IntPtr addr = Marshal.ReadIntPtr(CEEInfoVTable + IntPtr.Size * i);
            //        if(Marshal.ReadIntPtr(addr) == destAddress)
            //        {
            //            Console.WriteLine("Found ConstructStringLiteralOffset: " + i.ToString("X"));
            //            Debugger.Break();
            //        }
            //    }
            //}

            //Console.WriteLine("Finish");
            //Console.ReadKey();

            //PrepareMethod in previous versions of .NET Core 3.0, will raise StackOverFlowException
            ResolveToken(default, default);
        }

        public static uint GetMethodDefFromMethod(IntPtr hMethod)
        {
            return _getMethodDefFromMethod(CEEInfoVTable, hMethod);
        }

        public static void ResolveToken(IntPtr thisHandle, IntPtr pResolvedToken)
        {
            if (thisHandle == IntPtr.Zero)
                return;

            _resolveToken(thisHandle, pResolvedToken);
        }

        public static InfoAccessType ConstructStringLiteral(IntPtr thisHandle, IntPtr hModule, int metadataToken, IntPtr ptrString)
        {
            return _constructStringLiteral(thisHandle, hModule, metadataToken, ptrString);
        }
    }
}