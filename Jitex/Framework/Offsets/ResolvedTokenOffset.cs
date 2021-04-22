namespace Jitex.Framework.Offsets
{
    internal static class ResolvedTokenOffset
    {
        public static int Context { get; }
        public static int Scope { get; }
        public static int Token { get; }
        public static int Type { get; }

        public static int HClass { get; }
        public static int HMethod { get; }
        public static int HField { get; }

        static ResolvedTokenOffset()
        {
            Context = 0x0;
            Scope = 8;
            Token = 16;
            Type = 20;
            HClass = 24;
            HMethod = 32;
            HField = 40;
        }
    }
}
