using System;

namespace Jitex.Runtime
{
    internal static class MethodInfoOffset
    {
        public static int MethodDesc { get; }
        public static int Module { get; }
        public static int ILCode { get; }
        public static int ILCodeSize { get; }
        public static int MaxStack { get; }
        public static int Locals { get; }

        static MethodInfoOffset()
        {
            MethodDesc = 0x0;
            Module = 0x8;
            ILCode = 0x10;
            ILCodeSize = 0x18;
            MaxStack = 0x1C;
            Locals = 0x98;
        }
    }
}
