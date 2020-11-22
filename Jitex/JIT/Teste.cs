// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Jitex.JIT.CorInfo
{
    internal struct CORINFO_SIG_INST
    {
        public uint classInstCount;
        public IntPtr classInst; // (representative, not exact) instantiation for class type variables in signature
        public uint methInstCount;
        public IntPtr methInst; // (representative, not exact) instantiation for method type variables in signature
    }

    internal unsafe struct CORINFO_SIG_INFO
    {
        public CorInfoCallConv callConv;
        public IntPtr retTypeClass; // if the return type is a value class, this is its handle (enums are normalized)
        public IntPtr retTypeSigClass; // returns the value class as it is in the sig (enums are not converted to primitives)
        public byte _retType;
        public CorInfoSigInfoFlags flags; // used by IL stubs code
        public ushort numArgs;
        public CORINFO_SIG_INST sigInst; // information about how type variables are being instantiated in generic code
        public IntPtr args;
        public IntPtr pSig;
        public uint cbSig;
        public IntPtr scope; // passed to getArgClass
        public uint token;
    }

    internal unsafe struct CORINFO_RESOLVED_TOKEN
    {
        //
        // [In] arguments of resolveToken
        //
        public IntPtr tokenContext; //Context for resolution of generic arguments
        public IntPtr tokenScope;
        public int token; //The source token
        public TokenKind tokenType;

        //
        // [Out] arguments of resolveToken.
        // - Type handle is always non-NULL.
        // - At most one of method and field handles is non-NULL (according to the token type).
        // - Method handle is an instantiating stub only for generic methods. Type handle
        //   is required to provide the full context for methods in generic types.
        //
        public IntPtr hClass;
        public IntPtr hMethod;
        public IntPtr hField;

        //
        // [Out] TypeSpec and MethodSpec signatures for generics. NULL otherwise.
        //
        public byte* pTypeSpec;
        public uint cbTypeSpec;
        public byte* pMethodSpec;
        public uint cbMethodSpec;
    }

    internal struct CORINFO_CONSTRUCT_STRING
    {
        public IntPtr HandleModule { get; set; }
        public int MetadataToken { get; set; }
        public IntPtr PPValue { get; set; }
        
        public CORINFO_CONSTRUCT_STRING(IntPtr handleModule, int metadataToken, IntPtr ppValue)
        {
            HandleModule = handleModule;
            MetadataToken = metadataToken;
            PPValue = ppValue;
        }
    }

    // The enumeration is returned in 'getSig'

    internal enum CorInfoCallConv
    {
        CORINFO_CALLCONV_DEFAULT = 0x0,
        CORINFO_CALLCONV_C = 0x1,
        CORINFO_CALLCONV_STDCALL = 0x2,
        CORINFO_CALLCONV_THISCALL = 0x3,
        CORINFO_CALLCONV_FASTCALL = 0x4,
        CORINFO_CALLCONV_VARARG = 0x5,
        CORINFO_CALLCONV_FIELD = 0x6,
        CORINFO_CALLCONV_LOCAL_SIG = 0x7,
        CORINFO_CALLCONV_PROPERTY = 0x8,
        CORINFO_CALLCONV_UNMANAGED = 0x9,
        CORINFO_CALLCONV_NATIVEVARARG = 0xb,    // used ONLY for IL stub PInvoke vararg calls

        CORINFO_CALLCONV_MASK = 0x0f,     // Calling convention is bottom 4 bits
        CORINFO_CALLCONV_GENERIC = 0x10,
        CORINFO_CALLCONV_HASTHIS = 0x20,
        CORINFO_CALLCONV_EXPLICITTHIS = 0x40,
        CORINFO_CALLCONV_PARAMTYPE = 0x80,     // Passed last. Same as CORINFO_GENERICS_CTXT_FROM_PARAMTYPEARG
    }

    internal enum CorInfoSigInfoFlags : byte
    {
        CORINFO_SIGFLAG_IS_LOCAL_SIG = 0x01,
        CORINFO_SIGFLAG_IL_STUB = 0x02,
        CORINFO_SIGFLAG_SUPPRESS_GC_TRANSITION = 0x04,
    };

    // These are returned from getMethodOptions
    internal enum CorInfoOptions
    {
        CORINFO_OPT_INIT_LOCALS = 0x00000010, // zero initialize all variables

        CORINFO_GENERICS_CTXT_FROM_THIS = 0x00000020, // is this shared generic code that access the generic context from the this pointer?  If so, then if the method has SEH then the 'this' pointer must always be reported and kept alive.
        CORINFO_GENERICS_CTXT_FROM_METHODDESC = 0x00000040, // is this shared generic code that access the generic context from the ParamTypeArg(that is a MethodDesc)?  If so, then if the method has SEH then the 'ParamTypeArg' must always be reported and kept alive. Same as CORINFO_CALLCONV_PARAMTYPE
        CORINFO_GENERICS_CTXT_FROM_METHODTABLE = 0x00000080, // is this shared generic code that access the generic context from the ParamTypeArg(that is a MethodTable)?  If so, then if the method has SEH then the 'ParamTypeArg' must always be reported and kept alive. Same as CORINFO_CALLCONV_PARAMTYPE

        CORINFO_GENERICS_CTXT_MASK = CORINFO_GENERICS_CTXT_FROM_THIS |
                                     CORINFO_GENERICS_CTXT_FROM_METHODDESC |
                                     CORINFO_GENERICS_CTXT_FROM_METHODTABLE,
        CORINFO_GENERICS_CTXT_KEEP_ALIVE = 0x00000100, // Keep the generics context alive throughout the method even if there is no explicit use, and report its location to the CLR
    }

    internal struct CORINFO_METHOD_INFO
    {
        /// <summary>
        /// Instance MethodDesc.
        /// </summary>
        public IntPtr ftn;
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

    //
    // what type of code region we are in
    //
    internal enum CorInfoRegionKind
    {
        CORINFO_REGION_NONE,
        CORINFO_REGION_HOT,
        CORINFO_REGION_COLD,
        CORINFO_REGION_JIT,
    }
}