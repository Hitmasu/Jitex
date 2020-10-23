using System;

namespace Jitex.Runtime.Offsets
{
    internal static class CEEInfoOffset
    {
        internal static int ResolveToken { get; private set; }
        internal static int GetMethodDefFromMethod { get; private set; }
        internal static int ConstructStringLiteral { get; private set; }

        static CEEInfoOffset()
        {
            RuntimeFramework framework = RuntimeFramework.GetFramework();
            bool isNetCore = framework.IsCore;

            if (isNetCore)
                ReadNETCore(framework.FrameworkVersion);
        }

        private static void ReadNETCore(Version version)
        {
            if (version >= new Version(3, 1, 1))
            {
                ResolveToken = 0x1C;
                GetMethodDefFromMethod = 0x74;
                ConstructStringLiteral = 0x97;
            }
            else if (version >= new Version(2,1,0))
            {
                ResolveToken = 0x1C;
                GetMethodDefFromMethod = 0x70;
                ConstructStringLiteral = 0x92;
            }
            else
            {
                ResolveToken = 0x1A;
                GetMethodDefFromMethod = 0x69;
                ConstructStringLiteral = 0x8B;
            }
        }
    }
}
