using System;

namespace Jitex.Framework.Offsets
{
    internal static class CEEInfoOffset
    {
        public static int ResolveToken { get; private set; }

        public static int ConstructStringLiteral { get; private set; }

        static CEEInfoOffset()
        {
            RuntimeFramework framework = RuntimeFramework.Framework;
            ReadOffset(framework.IsCore, framework.FrameworkVersion);
        }

        private static void ReadOffset(bool isCore, Version version)
        {
            if (isCore && version >= new Version(7, 0, 0))
            {
                ResolveToken = 0x1D;
                ConstructStringLiteral = 0x95;
            }
            else if (isCore && version >= new Version(6, 0, 0))
            {
                ResolveToken = 0x1D;
                ConstructStringLiteral = 0x92;
            }
            else if (isCore && version >= new Version(5, 0, 0))
            {
                ResolveToken = 0x1B;
                ConstructStringLiteral = 0x90;
            }
            else if (isCore && version >= new Version(3, 0, 0)) //.NET Core 3.0 or higher
            {
                ResolveToken = 0x1C;
                ConstructStringLiteral = 0x97;
            }
            else if ((isCore && version >= new Version(2, 1, 0)) ||
                     (!isCore && version >= new Version(4, 0, 30319))) //.NET Core 2.1 | .NET Framework 4.6.1
            {
                ResolveToken = 0x1C;
                ConstructStringLiteral = 0x92;
            }
            else //.NET Core 2.0
            {
                ResolveToken = 0x1A;
                ConstructStringLiteral = 0x8B;
            }
        }
    }
}