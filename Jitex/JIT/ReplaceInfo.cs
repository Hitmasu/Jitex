using System;

namespace Jitex.JIT
{
    public class ReplaceInfo
    {
        public ReplaceInfo(ReplaceMode mode, byte[] body)
        {
            Mode = mode;
            Body = body;
        }

        public enum ReplaceMode
        {
            IL,
            ASM
        }

        public ReplaceMode Mode { get; }
        public byte[] Body { get; }
    }
}
