namespace Jitex.Framework.Offsets
{
    internal static class SigInfoOffset
    {
        internal static int NumArgs { get; }
        internal static int Args { get; }
        internal static int Signature { get; }

        static SigInfoOffset()
        {
            NumArgs = 0x1A;
            Args = 0x40;
            Signature = 0x48;
        }
    }
}
