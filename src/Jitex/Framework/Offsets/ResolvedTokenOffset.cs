using System;

namespace Jitex.Framework.Offsets
{
    internal static class ResolvedTokenOffset
    {
        public static int SourceOffset { get; private set; }
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
            Scope = 0x8;
            Token = 0x10;
            Type = 0x14;
            HClass = 0x18;
            HMethod = 0x20;
            HField = 0x28;
            
            RuntimeFramework framework = RuntimeFramework.Framework;
            ReadOffset(framework.IsCore, framework.FrameworkVersion);
        }

        private static void ReadOffset(bool isCore, Version version)
        {
            if (isCore && version >= new Version(7, 0, 0))
                SourceOffset = 5;
            else
                SourceOffset = 2;
        }
    }
}