namespace Jitex.JIT.CORTypes
{
    internal static class Enums
    {
        // ReSharper disable UnusedMember.Local
        public enum CorInfoOptions
        {
            /// <summary>
            /// zero initialize all variables
            /// </summary>
            CORINFO_OPT_INIT_LOCALS = 0x00000010, 

            /// <summary>
            /// Is this shared generic code that access the generic context from the this pointer?  If so, then if the method has SEH then the 'this' pointer must always be reported and kept alive
            /// </summary>
            CORINFO_GENERICS_CTXT_FROM_THIS = 0x00000020,

            /// <summary>
            /// Is this shared generic code that access the generic context from the ParamTypeArg(that is a MethodDesc)?  If so, then if the method has SEH then the 'ParamTypeArg' must always be reported and kept alive. Same as CORINFO_CALLCONV_PARAMTYPE
            /// </summary>
            CORINFO_GENERICS_CTXT_FROM_METHODDESC = 0x00000040,

            /// <summary>
            /// Is this shared generic code that access the generic context from the ParamTypeArg(that is a MethodTable)?  If so, then if the method has SEH then the 'ParamTypeArg' must always be reported and kept alive. Same as CORINFO_CALLCONV_PARAMTYPE
            /// </summary>
            CORINFO_GENERICS_CTXT_FROM_METHODTABLE = 0x00000080, 

            CORINFO_GENERICS_CTXT_MASK = (CORINFO_GENERICS_CTXT_FROM_THIS |
                                          CORINFO_GENERICS_CTXT_FROM_METHODDESC |
                                          CORINFO_GENERICS_CTXT_FROM_METHODTABLE),

            /// <summary>
            /// Keep the generics context alive throughout the method even if there is no explicit use, and report its location to the CLR
            /// </summary>
            CORINFO_GENERICS_CTXT_KEEP_ALIVE = 0x00000100
        };

        public enum CorInfoRegionKind
        {
            
            CORINFO_REGION_NONE,
            CORINFO_REGION_HOT,
            CORINFO_REGION_COLD,
            CORINFO_REGION_JIT,
        };

        public enum CorInfoType : byte
        {
            CORINFO_TYPE_UNDEF = 0x0,
            CORINFO_TYPE_VOID = 0x1,
            CORINFO_TYPE_BOOL = 0x2,
            CORINFO_TYPE_CHAR = 0x3,
            CORINFO_TYPE_BYTE = 0x4,
            CORINFO_TYPE_UBYTE = 0x5,
            CORINFO_TYPE_SHORT = 0x6,
            CORINFO_TYPE_USHORT = 0x7,
            CORINFO_TYPE_INT = 0x8,
            CORINFO_TYPE_UINT = 0x9,
            CORINFO_TYPE_LONG = 0xa,
            CORINFO_TYPE_ULONG = 0xb,
            CORINFO_TYPE_NATIVEINT = 0xc,
            CORINFO_TYPE_NATIVEUINT = 0xd,
            CORINFO_TYPE_FLOAT = 0xe,
            CORINFO_TYPE_DOUBLE = 0xf,
            
            /// <summary>
            /// Not used, should remove
            /// </summary>
            CORINFO_TYPE_STRING = 0x10,
            CORINFO_TYPE_PTR = 0x11,
            CORINFO_TYPE_BYREF = 0x12,
            CORINFO_TYPE_VALUECLASS = 0x13,
            CORINFO_TYPE_CLASS = 0x14,
            CORINFO_TYPE_REFANY = 0x15,
            
            /// <summary>
            /// Is for a generic type variable.
            /// Generic type variables only appear when the JIT is doing
            /// verification (not NOT compilation) of generic code
            /// for the EE, in which case we're running
            /// the JIT in "import only" mode.
            /// </summary>
            CORINFO_TYPE_VAR = 0x16,
            CORINFO_TYPE_COUNT, // number of jit types
        };

        public enum CorInfoCallConv
        {
            // These correspond to CorCallingConvention

            CORINFO_CALLCONV_DEFAULT = 0x0,
            CORINFO_CALLCONV_C = 0x1,
            CORINFO_CALLCONV_STDCALL = 0x2,
            CORINFO_CALLCONV_FASTCALL = 0x4,
            CORINFO_CALLCONV_VARARG = 0x5,
            CORINFO_CALLCONV_FIELD = 0x6,
            CORINFO_CALLCONV_LOCAL_SIG = 0x7,
            CORINFO_CALLCONV_PROPERTY = 0x8,
            CORINFO_CALLCONV_NATIVEVARARG = 0xb, // used ONLY for IL stub PInvoke vararg calls

            CORINFO_CALLCONV_MASK = 0x0f, // Calling convention is bottom 4 bits
            CORINFO_CALLCONV_GENERIC = 0x10,
            CORINFO_CALLCONV_HASTHIS = 0x20,
            CORINFO_CALLCONV_EXPLICITTHIS = 0x40,
            CORINFO_CALLCONV_PARAMTYPE = 0x80, // Passed last. Same as CORINFO_GENERICS_CTXT_FROM_PARAMTYPEARG
        };
        // ReSharper restore UnusedMember.Local
    }
}