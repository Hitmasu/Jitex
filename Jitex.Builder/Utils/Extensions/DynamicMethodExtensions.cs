using System.Reflection.Emit;

namespace Jitex.Builder.Utils.Extensions
{
    public static class DynamicMethodExtensions
    {
        public static byte[] GetILBytes(this DynamicMethod dynamicMethod)
        {
            ILGenerator generator = dynamicMethod.GetILGenerator();
            return generator.GetILBytes();
        }
    }
}