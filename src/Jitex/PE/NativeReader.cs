using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Jitex.JIT.CorInfo;
using Jitex.Utils;
using Jitex.Utils.NativeAPI.Windows;

namespace Jitex.PE
{
    public class NativeReader
    {
        private static readonly IDictionary<IntPtr, ImageInfo> Images = new Dictionary<IntPtr, ImageInfo>();

        private readonly IntPtr _base;
        private readonly int _size;
        private int _entryIndexSize;
        private int _nElements;
        private int _baseOffset;

        private protected ImageInfo Image { get; }

        public NativeReader(Module module)
        {
            IntPtr moduleHandle = AppModules.GetAddressFromModule(module);

            //if (!Images.TryGetValue(moduleHandle, out ImageInfo image))
            //{
            foreach (ProcessModule pModule in Process.GetCurrentProcess().Modules)
            {
                if (pModule.FileName == module.FullyQualifiedName)
                {
                    _base = pModule.BaseAddress;
                    _size = pModule.ModuleMemorySize;
                    break;
                }
            }

            LoadImage(module);
            //Images.Add(moduleHandle, image);
            //}
            //else
            //{
            //    _base = image!.BaseAddress;
            //    _size = image.Size;
            //    _nElements = (int) image.NumberOfElements;
            //    _entryIndexSize = image.EntryIndexSize;
            //}
        }

        private ImageInfo LoadImage(Module module)
        {
            uint virtualAddress = GetRTRVirtualAddress();

            if (virtualAddress != 0)
            {
                uint val;

                unsafe
                {
                    _baseOffset = DecodeUnsigned((int)virtualAddress, &val);
                }

                _nElements = (int)(val >> 2);
                _entryIndexSize = (byte)(val & 3);
                return new ImageInfo(module, _base, _size, true, default, (uint)_nElements, (byte)_entryIndexSize);
            }

            return new ImageInfo(module, _base, _size, false);
        }

        private unsafe uint GetRTRVirtualAddress()
        {
            ReadOnlySpan<byte> image = new ReadOnlySpan<byte>(_base.ToPointer(), _size);

            for (int i = 0; i < _size; i++)
            {
                if (image[i] == 'R' && image[i + 1] == 'T' && image[i + 2] == 'R')
                {
                    IntPtr startHeader = _base + i;
                    READYTORUN_HEADER header = Unsafe.Read<READYTORUN_HEADER>(startHeader.ToPointer());

                    IntPtr startSection = startHeader + sizeof(READYTORUN_HEADER);
                    ReadOnlySpan<READYTORUN_SECTION> sections = new ReadOnlySpan<READYTORUN_SECTION>(startSection.ToPointer(), (int)header.CoreHeader.NumberOfSections);

                    foreach (READYTORUN_SECTION section in sections)
                    {
                        if (section.Type == ReadyToRunSectionType.MethodDefEntryPoints)
                            return section.Section.VirtualAddress;
                    }
                }
            }

            return 0;
        }

        protected unsafe int DecodeUnsigned(int offset, uint* pValue)
        {
            if (offset >= _size)
                throw new BadImageFormatException();

            uint val = *(byte*)(_base + offset);
            if ((val & 1) == 0)
            {
                *pValue = (val >> 1);
                offset += 1;
            }
            else
            if ((val & 2) == 0)
            {
                if (offset + 1 >= _size)
                    throw new BadImageFormatException();
                *pValue = ((val >> 2) |
                                  (((uint)*(byte*)(_base + offset + 1)) << 6));
                offset += 2;
            }
            else
            if ((val & 4) == 0)
            {
                if (offset + 2 >= _size)
                    throw new BadImageFormatException();
                *pValue = (val >> 3) |
                          (((uint)*(byte*)(_base + offset + 1)) << 5) |
                          (((uint)*(byte*)(_base + offset + 2)) << 13);
                offset += 3;
            }
            else
            if ((val & 8) == 0)
            {
                if (offset + 3 >= _size)
                    throw new BadImageFormatException();
                *pValue = (val >> 4) |
                          (((uint)(byte*)(_base + offset + 1)) << 4) |
                          (((uint)(byte*)(_base + offset + 2)) << 12) |
                          (((uint)(byte*)(_base + offset + 3)) << 20);
                offset += 4;
            }
            else
            if ((val & 16) == 0)
            {
                *pValue = ReadUInt32(offset + 1);
                offset += 5;
            }
            else
            {
                throw new BadImageFormatException();
            }

            return offset;
        }

        protected unsafe uint ReadUInt32(int offset)
        {
            if (offset < 0 || offset + 3 >= _size)
                throw new BadImageFormatException();

            return *(uint*)(_base + offset); // Assumes little endian and unaligned access
        }

        unsafe byte ReadUInt8(int offset)
        {
            if (offset >= _size)
                throw new BadImageFormatException();
            return *(byte*)(_base + offset); // Assumes little endian and unaligned access
        }

        unsafe ushort ReadUInt16(int offset)
        {
            if (offset < 0 || offset + 1 >= _size)
                throw new BadImageFormatException();

            return *(ushort*)(_base + offset); // Assumes little endian and unaligned access
        }

        public unsafe bool TryGetAt(uint index)
        {
            const uint _blockSize = 16;

            if (index >= _nElements)
                return false;

            uint offset;
            if (_entryIndexSize == 0)
            {
                offset = ReadUInt8(_baseOffset + (int)(index / _blockSize));
            }
            else if (_entryIndexSize == 1)
            {
                offset = ReadUInt16(_baseOffset + (int)(2 * (index / _blockSize)));
            }
            else
            {
                offset = ReadUInt32(_baseOffset + (int)(4 * (index / _blockSize)));
            }
            offset += (uint)_baseOffset;
            Kernel32.VirtualProtect(_base + (int) offset, 1, Kernel32.MemoryProtection.EXECUTE_READ_WRITE);
            Marshal.WriteByte(_base + (int)offset, 0);
            for (uint bit = _blockSize >> 1; bit > 0; bit >>= 1)
            {
                uint val;
                uint offset2 = (uint)DecodeUnsigned((int)offset, &val);
                if ((index & bit) != 0)
                {
                    Console.WriteLine("First");
                    if ((val & 2) != 0)
                    {
                        Console.WriteLine("Second if");
                        offset = offset + (val >> 2);
                        continue;
                    }
                }
                else
                {
                    if ((val & 1) != 0)
                    {
                        Console.WriteLine("Third if");
                        offset = offset2;
                        continue;
                    }
                }

                // Not found
                if ((val & 3) == 0)
                {
                    Console.WriteLine("Four if");
                    // Matching special leaf node?
                    if ((val >> 2) == (index & (_blockSize - 1)))
                    {
                        Console.WriteLine("Five if");
                        offset = offset2;
                        break;
                    }
                }
                return false;
            }
            
            return true;
        }
    }
}
