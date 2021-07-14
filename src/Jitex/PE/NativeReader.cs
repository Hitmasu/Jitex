using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Jitex.JIT.CorInfo;
using Jitex.Utils;
using Jitex.Utils.Extension;
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
        private const uint BlockSize = 16;

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
                                  ((uint)*(byte*)(_base + offset + 1) << 6));
                offset += 2;
            }
            else
            if ((val & 4) == 0)
            {
                if (offset + 2 >= _size)
                    throw new BadImageFormatException();
                *pValue = (val >> 3) |
                          ((uint)*(byte*)(_base + offset + 1) << 5) |
                          ((uint)*(byte*)(_base + offset + 2) << 13);
                offset += 3;
            }
            else
            if ((val & 8) == 0)
            {
                if (offset + 3 >= _size)
                    throw new BadImageFormatException();
                *pValue = (val >> 4) |
                          ((uint)(byte*)(_base + offset + 1) << 4) |
                          ((uint)(byte*)(_base + offset + 2) << 12) |
                          ((uint)(byte*)(_base + offset + 3) << 20);
                offset += 4;
            }
            else
            if ((val & 16) == 0)
            {
                *pValue = MemoryHelper.ReadUnaligned<uint>(_base, offset + 1);
                offset += 5;
            }
            else
            {
                throw new BadImageFormatException();
            }

            return offset;
        }

        public unsafe bool IsReadyToRun(MethodBase method)
        {
            int index = method.GetRID() - 1;

            if (index >= _nElements)
                return false;

            uint offset = _entryIndexSize switch
            {
                0 => MemoryHelper.ReadUnaligned<byte>(_base, _baseOffset + (int)(index / BlockSize)),
                1 => MemoryHelper.ReadUnaligned<ushort>(_base, _baseOffset + (int)(2 * (index / BlockSize))),
                _ => MemoryHelper.ReadUnaligned<uint>(_base, _baseOffset + (int)(4 * (index / BlockSize)))
            };

            offset += (uint)_baseOffset;

            for (uint bit = BlockSize >> 1; bit > 0; bit >>= 1)
            {
                uint val;
                uint offset2 = (uint)DecodeUnsigned((int)offset, &val);
                if ((index & bit) != 0)
                {
                    if ((val & 2) != 0)
                    {
                        offset = offset + (val >> 2);
                        continue;
                    }
                }
                else
                {
                    if ((val & 1) != 0)
                    {
                        offset = offset2;
                        continue;
                    }
                }

                // Not found
                if ((val & 3) == 0)
                {
                    // Matching special leaf node?
                    if ((val >> 2) == (index & (BlockSize - 1)))
                    {
                        break;
                    }
                }
                return false;
            }

            return true;
        }

        public byte WriteEntry(MethodBase method, byte value)
        {
            int index = method.GetRID() - 1;

            uint offset = _entryIndexSize switch
            {
                0 => MemoryHelper.ReadUnaligned<byte>(_base, _baseOffset + (int)(index / BlockSize)),
                1 => MemoryHelper.ReadUnaligned<ushort>(_base, _baseOffset + (int)(2 * (index / BlockSize))),
                _ => MemoryHelper.ReadUnaligned<uint>(_base, _baseOffset + (int)(4 * (index / BlockSize)))
            };

            offset += (uint)_baseOffset;

            byte oldByte = MemoryHelper.Read<byte>(_base, (int) offset);
            MemoryHelper.UnprotectWrite(_base, (int)offset, value);

            return oldByte;
        }
    }
}
