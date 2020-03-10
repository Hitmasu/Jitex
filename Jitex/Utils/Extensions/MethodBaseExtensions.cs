using System.Reflection;
using System.Reflection.Emit;

namespace Jitex.Utils.Extensions
{
    internal static class MethodBaseExtensions
    {
        public static byte[] GetILBytes(this MethodBase methodBase)
        {
            if (methodBase is DynamicMethod dynamicMethod)
            {
                return dynamicMethod.GetILBytes();
            }

            return methodBase.GetMethodBody().GetILAsByteArray();
        }
    }
}
