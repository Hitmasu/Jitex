namespace Jitex.JIT.CorInfo
{
    public enum TokenKind
    {
        Class = 0x01,
        Method = 0x02,
        Field = 0x04,
        Mask = 0x07,
        String = 0x08,

        // token comes from CEE_LDTOKEN
        LdToken = 0x17,

        // token comes from CEE_CASTCLASS or CEE_ISINST
        Casting = 0x21,

        // token comes from CEE_NEWARR
        Newarr = 0x41,

        // token comes from CEE_BOX
        Box = 0x81,

        // token comes from CEE_CONSTRAINED
        Constrained = 0x101,

        // token comes from CEE_NEWOBJ
        Newobj = 0x202,

        // token comes from CEE_LDVIRTFTN
        Ldnvirtftn = 0x402,
    };
}
