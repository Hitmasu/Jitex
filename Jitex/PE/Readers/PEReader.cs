using System;

namespace Jitex.PE.Readers
{
    internal class PEReader : ReaderBase<PE>
    {
        private PE _pe;

        public PEReader(IntPtr address) : base(address)
        {
            _pe = new PE(address);
        }

        public override PE Read()
        {
            _pe.Signature = ReadInt32();
            _pe.MajorVersion = ReadInt16();
            _pe.MinorVersion = ReadInt16();
            _pe.Reserved = ReadInt32();

            int length = ReadInt32();
            length = (int) Math.Round((double) (length / 4)) * 4;

            _pe.Version = ReadUTF8(length);

            int boundary = (int) Address.ToInt64() % 4;
            Address += 4 - boundary;

            _pe.Flags = ReadInt16();
            _pe.Streams = ReadInt16();
            
            MHReader mhReader = new MHReader(Address,_pe.Streams);
            _pe.MetadataHeader = mhReader.Read();

            return _pe;
        }
    }
}