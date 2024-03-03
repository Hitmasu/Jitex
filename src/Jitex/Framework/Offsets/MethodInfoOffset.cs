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
            Scope = MethodDesc + IntPtr.Size;
            ILCode = Scope + IntPtr.Size;
            ILCodeSize = ILCode + IntPtr.Size;
            MaxStack = ILCodeSize + sizeof(uint);
            EHCount = MaxStack + sizeof(uint);

            if (version >= new Version(6, 0, 0))
                Locals = OSHelper.IsX86 ? 0x50 : 0xA0;
            else
                Locals = OSHelper.IsX86 ? 0x4C : 0x98;
        }
    }
}