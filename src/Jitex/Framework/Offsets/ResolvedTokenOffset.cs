using System;
using Jitex.JIT.CorInfo;
using Jitex.Utils;

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
            Scope = Context + IntPtr.Size;
            Token = Scope + IntPtr.Size;
            Type = Token + sizeof(short);
            HClass = Type + sizeof(TokenKind);
            HMethod = HClass + IntPtr.Size;
            HField = HMethod + IntPtr.Size;

            RuntimeFramework framework = RuntimeFramework.Framework;
            ReadOffset(framework.IsCore, framework.FrameworkVersion);
        }

        private static void ReadOffset(bool isCore, Version version)
        {
            if (isCore && version >= new Version(8, 0, 0))
                SourceOffset = 2;
            else if (isCore && version >= new Version(7, 0, 0))
                SourceOffset = 5;
            else
                SourceOffset = 2;
        }
    }
}