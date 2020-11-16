using System.Diagnostics;

namespace Jitex.Utils
{
    public static class ProcessInfo
    {
        /// <summary>
        /// Process Id.
        /// </summary>
        public static int PID { get; }

        static ProcessInfo()
        {
            PID = Process.GetCurrentProcess().Id;
        }
    }
}
