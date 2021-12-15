using System;
using System.Reflection;

namespace Jitex.PE
{
    public class ImageInfo
    {
        public int MethodRefRows { get; internal set; }
        public int TypeRefRows { get; internal set; }

        public Module Module { get; internal set; }
        public IntPtr BaseAddress { get; internal set; }
        public int Size { get; internal set; }
        public uint NumberOfElements { get; internal set; }
        public byte EntryIndexSize { get; internal set; }
        public int BaseOffset { get; internal set; }

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

        public int GetNewMethodRefIndex()
        {
            const int memberRefBase = 0x0A000000;
            
            int newMemberRefIndex = ++MethodRefRows;
            return memberRefBase + newMemberRefIndex;
        }

        public int GetNewTypeRefIndex()
        {
            const int typeRefBase = 0x01000000;
            
            int newTypeRefIndex = ++TypeRefRows;
            return typeRefBase + newTypeRefIndex;
        }
    }
}