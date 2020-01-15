using System;

namespace Jitex.PE.Readers
{
    internal class MHReader : ReaderBase<MetadataHeader>
    {
        private readonly MetadataHeader _header;

        public MHReader(IntPtr address, int numberOfStreams) : base(address)
        {
            _header = new MetadataHeader(address, numberOfStreams);
        }

        public override MetadataHeader Read()
        {
            IntPtr nextAddress = Address;
            
            for (int i = 0; i < _header.Headers.Count; i++)
            {
                SHReader shReader = new SHReader(nextAddress);
                _header.Headers[i] = shReader.Read();
                nextAddress = shReader.Address;
            }

            return _header;
        }
    }
}