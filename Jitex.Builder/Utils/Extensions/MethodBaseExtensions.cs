using System.Reflection;
using System.Reflection.Emit;

namespace Jitex.Builder.Utils.Extensions
{
    public static class MethodBaseExtensions
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