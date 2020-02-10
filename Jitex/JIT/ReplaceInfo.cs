using MethodBody = Jitex.Builder.MethodBody;

namespace Jitex.JIT
{
    public class ReplaceInfo
    {

        public ReplaceInfo(byte[] byteCode)
        {
            ByteCode = byteCode;
        }

        public ReplaceInfo(MethodBody methodBody)
        {
            MethodBody = methodBody;
        }

        public enum ReplaceMode
        {
            IL,
            ASM
        }

        public MethodBody MethodBody { get; }
        public byte[] ByteCode { get; }
        public ReplaceMode Mode => ByteCode != null ? ReplaceMode.ASM : ReplaceMode.IL;
    }
}
