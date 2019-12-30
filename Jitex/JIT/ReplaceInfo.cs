using System;
using System.Collections.Generic;
using System.Text;

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

        public ReplaceMode Mode { get; set; }
        public byte[] Body { get; set; }
    }
}
