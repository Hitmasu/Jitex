using System;

namespace Jitex.PE
{
    public class PE : MetadataInfo
    {

        public int Signature { get; set; }
        public int MajorVersion { get; set; }
        public int MinorVersion { get; set; }
        public int Reserved { get; set; }
        public string Version { get; set; }
        public int Flags { get; set; }
        public int Streams { get; set; }
        public MetadataHeader MetadataHeader { get; set; }

        public PE(IntPtr address) : base(address)
        {
        }
    }
}