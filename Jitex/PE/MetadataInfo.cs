using System;

namespace Jitex.PE
{
    public abstract class MetadataInfo
    {
        protected MetadataInfo(IntPtr address)
        {
            Address = address;
        }

        public IntPtr Address { get; }
    }
}