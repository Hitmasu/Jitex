namespace Jitex.Builder.Method
{
    /// <summary>
    /// Type identifier used by CLR.
    /// </summary>
    /// <remarks>
    /// Type identifier used internally by CLR.
    /// Some of them are used only internally by CLR, not exposed to be used directly by developers.
    /// </remarks>
    public enum CorElementType : byte
    {
        /// <summary>
        /// Used internally.
        /// </summary>
        ELEMENT_TYPE_END = 0x00,

        /// <summary>
        /// A void type.
        /// </summary>
        ELEMENT_TYPE_VOID = 0x01,

        /// <summary>
        /// A Boolean type (bool).
        /// </summary>
        ELEMENT_TYPE_BOOLEAN = 0x02,

        /// <summary>
        /// A character type (char).
        /// </summary>
        ELEMENT_TYPE_CHAR = 0x03,

        /// <summary>
        /// A signed 1-byte integer (sbyte).
        /// </summary>
        ELEMENT_TYPE_I1 = 0x04,

        /// <summary>
        /// An unsigned 1-byte integer (byte).
        /// </summary>
        ELEMENT_TYPE_U1 = 0x05,

        /// <summary>
        /// A signed 2-byte integer (short).
        /// </summary>
        ELEMENT_TYPE_I2 = 0x06,

        /// <summary>
        /// An unsigned 2-byte integer (ushort).
        /// </summary>
        ELEMENT_TYPE_U2 = 0x07,

        /// <summary>
        /// A signed 4-byte integer (int|Int32).
        /// </summary>
        ELEMENT_TYPE_I4 = 0x08,

        /// <summary>
        /// An unsigned 4-byte integer (uint|Uint32).
        /// </summary>
        ELEMENT_TYPE_U4 = 0x09,

        /// <summary>
        /// A signed 8-byte integer (long|Int64).
        /// </summary>
        ELEMENT_TYPE_I8 = 0x0A,

        /// <summary>
        /// An unsigned 8-byte integer (ulong|Uint64).
        /// </summary>
        ELEMENT_TYPE_U8 = 0x0B,

        /// <summary>
        /// A 4-byte floating point (float).
        /// </summary>
        ELEMENT_TYPE_R4 = 0x0C,

        /// <summary>
        /// An 8-byte floating point (double).
        /// </summary>
        ELEMENT_TYPE_R8 = 0x0D,

        /// <summary>
        /// A System.String type (string).
        /// </summary>
        ELEMENT_TYPE_STRING = 0x0E,

        /// <summary>
        /// A pointer type modifier.
        /// </summary>
        ELEMENT_TYPE_PTR = 0x0F,

        /// <summary>
        /// A reference type modifier.
        /// </summary>
        ELEMENT_TYPE_BYREF = 0x10,

        /// <summary>
        /// A value type modifier (struct).
        /// </summary>
        ELEMENT_TYPE_VALUETYPE = 0x11,

        /// <summary>
        /// A class type modifier (class).
        /// </summary>
        ELEMENT_TYPE_CLASS = 0x12,

        /// <summary>
        /// A class variable type modifier.
        /// </summary>
        ELEMENT_TYPE_VAR = 0x13,

        /// <summary>
        /// A multi-dimensional array type modifier.
        /// </summary>
        ELEMENT_TYPE_ARRAY = 0x14,

        /// <summary>
        /// A type modifier for generic types.
        /// </summary>
        ELEMENT_TYPE_GENERICINST = 0x15,

        /// <summary>
        /// A typed reference.
        /// </summary>
        ELEMENT_TYPE_TYPEDBYREF = 0x16,

        /// <summary>
        /// Size of a native integer.
        /// </summary>
        ELEMENT_TYPE_I = 0x18,

        /// <summary>
        /// Size of an unsigned native integer.
        /// </summary>
        ELEMENT_TYPE_U = 0x19,

        /// <summary>
        /// A pointer to a function.
        /// </summary>
        ELEMENT_TYPE_FNPTR = 0x1B,

        /// <summary>
        /// A System.Object type (object).
        /// </summary>
        ELEMENT_TYPE_OBJECT = 0x1C,

        /// <summary>
        /// A single-dimensional, zero lower-bound array type modifier.
        /// </summary>
        ELEMENT_TYPE_SZARRAY = 0x1D,

        /// <summary>
        /// A method variable type modifier.
        /// </summary>
        ELEMENT_TYPE_MVAR = 0x1E,

        /// <summary>
        /// A C language required modifier.
        /// </summary>
        ELEMENT_TYPE_CMOD_REQD = 0x1F,

        /// <summary>
        /// A C language optional modifier.
        /// </summary>
        ELEMENT_TYPE_CMOD_OPT = 0x20,

        /// <summary>
        /// Used internally.
        /// </summary>
        ELEMENT_TYPE_INTERNAL = 0x21,

        /// <summary>
        /// An invalid type.
        /// </summary>
        ELEMENT_TYPE_MAX = 0x22,
        
        /// <summary>
        /// Used internally.
        /// </summary>
        ELEMENT_TYPE_MODIFIER = 0x40,

        /// <summary>
        /// A type modifier that is a sentinel for a list of a variable number of parameters.
        /// </summary>
        ELEMENT_TYPE_SENTINEL = 0x41,

        /// <summary>
        /// Used internally.
        /// </summary>
        ELEMENT_TYPE_PINNED = 0x45,
    }
}