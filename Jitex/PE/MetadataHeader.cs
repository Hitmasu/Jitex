using System;
using System.Collections;
using System.Collections.Generic;

namespace Jitex.PE
{
    public class MetadataHeader : MetadataInfo
    {
        public IList<StreamHeader> Headers { get; }
        
        public MetadataHeader(IntPtr address, int numberOfStreams) : base(address)
        {
            Headers = new List<StreamHeader>(numberOfStreams);
        }
    }
}