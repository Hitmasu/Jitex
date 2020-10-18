namespace Jitex.Runtime.Offsets
{
    internal static class SigInfoOffset
    {
        internal static int NumArgs { get; }
        internal static int Args { get; }
        internal static int Signature { get; }

        static SigInfoOffset()
        {
            NumArgs = 25;
            Args = 35;
            Signature = 43;
        }
    }
}
