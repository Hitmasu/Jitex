using System;
using Jitex.Utils;

namespace Jitex.Framework.Offsets
{
    internal static class SigInfoOffset
    {
        internal static int NumArgs { get; }
        internal static int Args { get; }
        internal static int Signature { get; }

        static SigInfoOffset()
        {
            NumArgs = OSHelper.IsX86 ? 0x16 : 0x1A;
            Args = OSHelper.IsX86 ? 0x28 : 0x40;
            Signature = Args + IntPtr.Size;
        }
    }
}