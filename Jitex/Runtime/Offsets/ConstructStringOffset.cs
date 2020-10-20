using System;
using System.Collections.Generic;
using System.Text;

namespace Jitex.Runtime.Offsets
{
    internal static class ConstructStringOffset
    {
        internal static int Module { get; }
        internal static int MetadataToken { get; }
        internal static int PPValue { get; }

        static ConstructStringOffset()
        {
            Module = 0x0;
            MetadataToken = 0x08;
            PPValue = 0x12;
        }
    }
}
