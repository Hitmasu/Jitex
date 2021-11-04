namespace Jitex.JIT.CorInfo
{
    public enum CorJitResult
    {
        // Note that I dont use FACILITY_NULL for the facility number,
        // we may want to get a 'real' facility number
        CORJIT_OK = 0 /*NO_ERROR*/,
        CORJIT_BADCODE = unchecked((int)0x80000001)/*MAKE_HRESULT(SEVERITY_ERROR, FACILITY_NULL, 1)*/,
        CORJIT_OUTOFMEM = unchecked((int)0x80000002)/*MAKE_HRESULT(SEVERITY_ERROR, FACILITY_NULL, 2)*/,
        CORJIT_INTERNALERROR = unchecked((int)0x80000003)/*MAKE_HRESULT(SEVERITY_ERROR, FACILITY_NULL, 3)*/,
        CORJIT_SKIPPED = unchecked((int)0x80000004)/*MAKE_HRESULT(SEVERITY_ERROR, FACILITY_NULL, 4)*/,
        CORJIT_RECOVERABLEERROR = unchecked((int)0x80000005)/*MAKE_HRESULT(SEVERITY_ERROR, FACILITY_NULL, 5)*/,
        CORJIT_IMPLLIMITATION = unchecked((int)0x80000006)/*MAKE_HRESULT(SEVERITY_ERROR,FACILITY_NULL, 6)*/,
    };
}
