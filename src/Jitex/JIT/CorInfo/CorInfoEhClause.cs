using System.Runtime.InteropServices;

namespace Jitex.JIT.CorInfo;

[StructLayout(LayoutKind.Sequential)]
public struct CorInfoEhClause
{
    public uint Flags;
    public uint TryOffset;
    public uint TryLength;
    public uint HandlerOffset;
    public uint HandlerLength;
}