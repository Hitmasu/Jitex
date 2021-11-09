using System;

namespace Jitex.Framework.Offsets
{
    internal static class MethodInfoOffset
    {
        public static int MethodDesc { get; private set; }
        public static int Scope { get; private set; }
        public static int ILCode { get; private set; }
        public static int ILCodeSize { get; private set; }
        public static int MaxStack { get; private set; }
        public static int EHCount { get; private set; }
        public static int Locals { get; private set; }

        static MethodInfoOffset()
        {
            RuntimeFramework framework = RuntimeFramework.Framework;
            ReadOffset(framework.FrameworkVersion);
        }

        private static void ReadOffset(Version version)
        {
            MethodDesc = 0x0;
            Scope = 0x8;
            ILCode = 0x10;
            ILCodeSize = 0x18;
            MaxStack = 0x1C;
            EHCount = 0x20;

            if (version >= new Version(6, 0, 0))
                Locals = 0xA0;
            else
                Locals = 0x98;
        }
    }
}
