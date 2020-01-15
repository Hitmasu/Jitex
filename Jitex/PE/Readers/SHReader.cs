using System;

namespace Jitex.PE.Readers
{
    internal class SHReader : ReaderBase<StreamHeader>
    {
        private readonly StreamHeader _header;

        public SHReader(IntPtr address) : base(address)
        {
            _header = new StreamHeader(address);
        }

        public override StreamHeader Read()
        {
            _header.Offseet = ReadInt32();
            _header.Size = ReadInt32();
            _header.Name = ReadANSI();
            
            return _header;
        }
    }
}