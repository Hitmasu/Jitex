using System.Reflection.Emit;

namespace Jitex.Utils.Extensions
{
    internal static class DynamicMethodExtensions
    {
        public static byte[] GetILBytes(this DynamicMethod dynamicMethod)
        {
            ILGenerator generator = dynamicMethod.GetILGenerator();
            return generator.GetILBytes();
        }
    }
}