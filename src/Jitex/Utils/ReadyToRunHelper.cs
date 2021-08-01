using System.Reflection;
using Jitex.PE;

namespace Jitex.Utils
{
    internal static class ReadyToRunHelper
    {
        public static bool DisableReadyToRun(MethodBase method)
        {
            if (!MethodIsReadyToRun(method))
                return false;

            return GetReader(method).DisableReadyToRun(method);
        }

        public static bool MethodIsReadyToRun(MethodBase method)
        {
            return GetReader(method).IsReadyToRun(method);
        }

        private static NativeReader GetReader(MethodBase method) => new(method.Module);
    }
}