using System;
using System.Reflection;

namespace Jitex.PE
{
    internal class ImageInfo
    {
        public Module Module { get; }
        public IntPtr BaseAddress { get; }
        public int Size { get; }
        public uint NumberOfElements { get; }
        public byte EntryIndexSize { get; }
        public int BaseOffset { get; set; }

        public ImageInfo(Module module)
        {
            Module = module;
        }

        public ImageInfo(Module module, IntPtr baseAddress, int size, int baseOffset, uint numberOfElements, byte entryIndexSize)
        {
            Module = module;
            BaseAddress = baseAddress;
            Size = size;
            NumberOfElements = numberOfElements;
            EntryIndexSize = entryIndexSize;
            BaseOffset = baseOffset;
        }
    }
}