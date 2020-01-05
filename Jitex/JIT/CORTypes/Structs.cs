using System;
using static Jitex.JIT.CORTypes.Enums;

namespace Jitex.JIT.CORTypes
{
    internal static class Structs
    {
        // ReSharper disable UnusedMember.Global
        // ReSharper disable InconsistentNaming
        public struct CORINFO_SIG_INFO
        {
            
            public CorInfoCallConv callConv;

            public IntPtr retTypeClass; // if the return type is a value class, this is its handle (enums are normalized)

            public IntPtr retTypeSigClass; // returns the value class as it is in the sig (enums are not converted to primitives)

            public CorInfoType retType;
            public byte flags;
            public ushort numArgs;

            public CORINFO_SIG_INST sigInst; // information about how type variables are being instantiated in generic code

            public IntPtr args;
            public IntPtr pSig;
            public uint cbSig;
            public IntPtr scope; // passed to getArgClass
            public uint token;
            public long garbage;
        }

        public struct CORINFO_SIG_INST
        {
            public uint classInstCount;
            public IntPtr classInst; // (representative, not exact) instantiation for class type variables in signature
            public uint methInstCount;
            public IntPtr methInst; // (representative, not exact) instantiation for method type variables in signature
        }

        public struct CORINFO_METHOD_INFO
        {
            /// <summary>
            /// Method Handle
            /// </summary>
            public IntPtr ftn;

            /// <summary>
            /// Module Handle
            /// </summary>
            public IntPtr scope; 


            public IntPtr ILCode;
            public int ILCodeSize;
            public uint maxStack;
            public uint EHcount;
            public CorInfoOptions options;
            public CorInfoRegionKind regionKind;
            public CORINFO_SIG_INFO args;
            public CORINFO_SIG_INFO locals;
        }
        // ReSharper restore InconsistentNaming
        // ReSharper restore UnusedMember.Global
    }
}