using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Jitex.PE.Readers
{
    internal abstract class ReaderBase<T>
    {
        public IntPtr Address { get; protected set; }

        protected ReaderBase(IntPtr address)
        {
            Address = address;
        }

        public abstract T Read();

        protected int ReadInt32()
        {
            int value = Marshal.ReadInt32(Address);
            Address += 4;
            return value;
        }
        
        protected int ReadInt16()
        {
            int value = Marshal.ReadInt16(Address);
            Address += 2;
            return value;
        }

        protected string ReadUTF8(int length)
        {
            string value = Marshal.PtrToStringUTF8(Address, length);
            Address += length;
            return value;
        }

        protected string ReadANSI()
        {
            string value = Marshal.PtrToStringAnsi(Address);
            Address += Encoding.ASCII.GetBytes(value).Length;
            return value;
        }
    }
}