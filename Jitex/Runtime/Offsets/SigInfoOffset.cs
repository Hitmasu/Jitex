namespace Jitex.Runtime.Offsets
{
    internal static class SigInfoOffset
    {
        internal static int NumArgs { get; }
        internal static int Args { get; }
        internal static int Signature { get; }

        static SigInfoOffset()
        {
            NumArgs = 21;
            Args = 32;
            Signature = 39;
        }
    }
}
