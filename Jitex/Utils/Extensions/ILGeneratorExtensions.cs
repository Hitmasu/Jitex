using System.Reflection;
using System.Reflection.Emit;

namespace Jitex.Utils.Extensions
{
    internal static class ILGeneratorExtensions
    {
        private static readonly FieldInfo m_ILStream;
        private static readonly FieldInfo m_length;
        private static readonly FieldInfo m_maxStackSize;

        static ILGeneratorExtensions()
        {
            m_ILStream = typeof(ILGenerator).GetField("m_ILStream", BindingFlags.Instance | BindingFlags.NonPublic);
            m_maxStackSize = typeof(ILGenerator).GetField("m_maxStackSize", BindingFlags.Instance | BindingFlags.NonPublic);
            m_length = typeof(ILGenerator).GetField("m_length", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        public static byte[] GetILBytes(this ILGenerator generator)
        {
            return (byte[]) m_ILStream.GetValue(generator);
        }

        public static int GetILLength(this ILGenerator generator)
        {
            return (int) m_length.GetValue(generator);
        }

        public static int GetMaxStackSize(this ILGenerator generator)
        {
            return (int) m_maxStackSize.GetValue(generator);
        }
    }
}