namespace Jitex.JIT.CorInfo
{
    internal enum InfoAccessType
    {
        IAT_VALUE,      // The info value is directly available
        IAT_PVALUE,     // The value needs to be accessed via an         indirection
        IAT_PPVALUE,    // The value needs to be accessed via a double   indirection
        IAT_RELPVALUE   // The value needs to be accessed via a relative indirection
    };
}
