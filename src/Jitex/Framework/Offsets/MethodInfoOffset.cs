using System;
using Jitex.Utils;

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
            Scope = OSHelper.IsX86 ? 0x4 : 0x8;
            ILCode = OSHelper.IsX86 ? 0x8 : 0x10;
            ILCodeSize = OSHelper.IsX86 ? 0xC : 0x18;
            MaxStack = OSHelper.IsX86 ? 0x10 : 0x1C;
            EHCount = OSHelper.IsX86 ? 0x14 : 0x20;

            if (version >= new Version(6, 0, 0))
                Locals = OSHelper.IsX86 ? 0x50 : 0xA0;
            else
                Locals = OSHelper.IsX86 ? 0x4C : 0x98;
        }
    }
}