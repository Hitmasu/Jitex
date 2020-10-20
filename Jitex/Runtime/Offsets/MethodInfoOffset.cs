using System;

namespace Jitex.Runtime
{
    internal static class MethodInfoOffset
    {
        public static int MethodDesc { get; private set; }
        public static int Module { get; private set; }
        public static int ILCode { get; private set; }
        public static int ILCodeSize { get; private set; }
        public static int MaxStack { get; private set; }
        public static int Locals { get; private set; }

        static MethodInfoOffset()
        {
            MethodDesc = 0x0;
            Module = 0x8;
            ILCode = 0x10;
            ILCodeSize = 0x18;
            MaxStack = 0x1C;

            //Version version = RuntimeFramework.GetFramework().FrameworkVersion;
            Locals = 0x98;
        }
    }
}
