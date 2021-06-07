using System;

namespace Jitex.JIT.CorInfo
{
    internal class ConstructString
    {
        public IntPtr HandleModule { get; }
        public int MetadataToken { get; }

        public ConstructString(IntPtr handleModule, int metadataToken)
        {
            HandleModule = handleModule;
            MetadataToken = metadataToken;
        }
    }
}