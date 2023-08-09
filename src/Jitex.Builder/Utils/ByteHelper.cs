using System;

namespace Jitex.Builder.Utils;

public static class ByteHelper
{
    public static byte[] GetBytes(dynamic obj)
    {
        return obj switch
        {
            bool b => BitConverter.GetBytes(b),
            byte by => new[] { by },
            sbyte by => new[] { by },
            char c => BitConverter.GetBytes(c),
            double db => BitConverter.GetBytes(db),
            float f => BitConverter.GetBytes(f),
            int i => BitConverter.GetBytes(i),
            uint ui => BitConverter.GetBytes(ui),
            long l => BitConverter.GetBytes(l),
            ulong ul => BitConverter.GetBytes(ul),
            short s => BitConverter.GetBytes(s),
            ushort us => BitConverter.GetBytes(us),
            _ => BitConverter.GetBytes(obj)
        };
    }
}