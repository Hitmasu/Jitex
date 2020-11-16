using System;
using System.Reflection;
using System.Runtime.InteropServices;
using Jitex.Runtime.Offsets;
using Jitex.Utils;

namespace Jitex.JIT.CorInfo
{
    internal class ConstructString
    {
        public IntPtr HandleModule { get; set; }
        public int MetadataToken { get; set; }
        public IntPtr PPValue { get; set; }

        public ConstructString(IntPtr handleModule, int metadataToken, IntPtr ppValue)
        {
            HandleModule = handleModule;
            MetadataToken = metadataToken;
            PPValue = ppValue;
        }
    }
}
