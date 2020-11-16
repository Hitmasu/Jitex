using System;
using System.Collections.Generic;
using System.Text;

namespace Jitex.Runtime.Offsets
{
    internal static class ResolvedTokenOffset
    {
        public static int Context { get; private set; }
        public static int Module { get; private set; }
        public static int Token { get; private set; }
        public static int Type { get; private set; }

        public static int HClass { get; private set; }
        public static int HMethod { get; private set; }
        public static int HField { get; private set; }

        static ResolvedTokenOffset()
        {
            Context = 0x0;
            Module = 8;
            Token = 16;
            Type = 20;
            HClass = 24;
            HMethod = 32;
            HField = 40;
        }
    }
}
