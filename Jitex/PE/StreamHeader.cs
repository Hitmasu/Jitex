using System;

namespace Jitex.PE
{
    public class StreamHeader : MetadataInfo
    {
        public int Offseet { get; set; }
        public int Size { get; set; }
        public string Name { get; set; }
        
        public StreamHeader(IntPtr address) : base(address)
        {
        }
    }
}