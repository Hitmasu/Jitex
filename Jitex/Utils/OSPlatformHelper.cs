using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Jitex.Utils
{
    public static class OSPlatformHelper
    {
        public static bool IsPosix => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    }
}
