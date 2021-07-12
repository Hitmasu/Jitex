using System;
using System.Reflection;

namespace Jitex.PE
{
    internal class ImageInfo
    {
        public Module Module { get; }
        public IntPtr BaseAddress { get; }
        public int Size { get; }
        public bool HasReadyToRun { get; }
        public IntPtr ReadyToRunHeader { get; }
        public uint NumberOfElements { get; }
        public byte EntryIndexSize { get; }

        public ImageInfo(Module module, IntPtr baseAddress, int size, bool hasReadyToRun)
        {
            Module = module;
            BaseAddress = baseAddress;
            Size = size;
            HasReadyToRun = hasReadyToRun;
        }

        public ImageInfo(Module module, IntPtr baseAddress, int size, bool hasReadyToRun, IntPtr readyToRunHeader, uint numberOfElements, byte entryIndexSize)
        {
            Module = module;
            BaseAddress = baseAddress;
            Size = size;
            HasReadyToRun = hasReadyToRun;
            ReadyToRunHeader = readyToRunHeader;
            NumberOfElements = numberOfElements;
            EntryIndexSize = entryIndexSize;
        }
    }
}