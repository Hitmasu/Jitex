using Microsoft.Extensions.Logging;

namespace Jitex.Utils
{
    public static class JitexLogger
    {
        internal static ILogger? Log { get; private set; }

        public static void SetLogger(ILoggerFactory loggerFactory)
        {
            Log = loggerFactory.CreateLogger("Jitex");
        }
    }
}