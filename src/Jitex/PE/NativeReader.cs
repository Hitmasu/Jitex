using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using dnlib.DotNet;
using Jitex.Framework;
using Jitex.JIT.CorInfo;
using Jitex.Utils;
using Jitex.Utils.Extension;

namespace Jitex.PE
{
    internal class NativeReader
    {
        private static readonly bool FrameworkSupportR2R;

        private static readonly ConcurrentDictionary<Module, ImageInfo> Images = new();

        private readonly bool _hasRtr;
        private readonly IntPtr _base;
        private readonly int _size;
        private int _entryIndexSize;
        private int _nElements;
        private int _baseOffset;
        private const uint BlockSize = 16;

        static NativeReader()
        {
            FrameworkSupportR2R = RuntimeFramework.Framework >= new Version(3, 0);
        }

        public NativeReader(Module module)
        {
            if (!Images.TryGetValue(module, out ImageInfo image))
            {
                (_base, _size) = OSHelper.GetModuleBaseAddress(module.FullyQualifiedName);

                image = LoadImage(module);
                Images.TryAdd(module, image);
                _hasRtr = image.NumberOfElements > 0;
            }
            else
            {
                _base = image!.BaseAddress;
                _size = image.Size;
                _nElements = (int) image.NumberOfElements;
                _entryIndexSize = image.EntryIndexSize;
                _baseOffset = image.BaseOffset;
                _hasRtr = image.NumberOfElements > 0;
            }
        }

        private ImageInfo LoadImage(Module module)
        {
            ModuleContext moduleContext = ModuleDef.CreateModuleContext();
            ModuleDefMD moduleDef = ModuleDefMD.Load(module, moduleContext);

            bool hasR2R = moduleDef.Metadata.ImageCor20Header.HasNativeHeader && FrameworkSupportR2R;

            if (hasR2R)
            {
                IntPtr startHeaderAddress = _base + (int) moduleDef.Metadata.ImageCor20Header.ManagedNativeHeader.VirtualAddress;
                uint virtualAddress = GetEntryPointSection(startHeaderAddress);

                if (virtualAddress == 0)
                    return new ImageInfo(module);

                uint val;

                unsafe
                {
                    _baseOffset = DecodeUnsigned((int) virtualAddress, &val);
                }

                _nElements = (int) (val >> 2);
                _entryIndexSize = (byte) (val & 3);
                return new ImageInfo(module, _base, _size, _baseOffset, (uint) _nElements, (byte) _entryIndexSize);
            }

            return new ImageInfo(module);
        }

        private static unsafe uint GetEntryPointSection(IntPtr startHeader)
        {
            READYTORUN_HEADER header = Unsafe.Read<READYTORUN_HEADER>(startHeader.ToPointer());

            if (header.Signature != 0x00525452) //Signature != 'RTR'
                return 0;

            IntPtr startSection = startHeader + sizeof(READYTORUN_HEADER);
            ReadOnlySpan<READYTORUN_SECTION> sections = new ReadOnlySpan<READYTORUN_SECTION>(startSection.ToPointer(), (int) header.CoreHeader.NumberOfSections);

            foreach (READYTORUN_SECTION section in sections)
            {
                if (section.Type == ReadyToRunSectionType.MethodDefEntryPoints)
                    return section.Section.VirtualAddress;
            }

            return 0;
        }

        protected unsafe int DecodeUnsigned(int offset, uint* pValue)
        {
            if (offset >= _size)
                throw new BadImageFormatException();

            uint val = *(byte*) (_base + offset);
            if ((val & 1) == 0)
            {
                *pValue = (val >> 1);
                offset += 1;
            }
            else if ((val & 2) == 0)
            {
                if (offset + 1 >= _size)
                    throw new BadImageFormatException();
                *pValue = ((val >> 2) |
                           ((uint) *(byte*) (_base + offset + 1) << 6));
                offset += 2;
            }
            else if ((val & 4) == 0)
            {
                if (offset + 2 >= _size)
                    throw new BadImageFormatException();
                *pValue = (val >> 3) |
                          ((uint) *(byte*) (_base + offset + 1) << 5) |
                          ((uint) *(byte*) (_base + offset + 2) << 13);
                offset += 3;
            }
            else if ((val & 8) == 0)
            {
                if (offset + 3 >= _size)
                    throw new BadImageFormatException();
                *pValue = (val >> 4) |
                          ((uint) (byte*) (_base + offset + 1) << 4) |
                          ((uint) (byte*) (_base + offset + 2) << 12) |
                          ((uint) (byte*) (_base + offset + 3) << 20);
                offset += 4;
            }
            else if ((val & 16) == 0)
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

        public bool IsReadyToRun(MethodBase method)
        {
            if (!_hasRtr)
                return false;

            int index = method.GetRID() - 1;

            if (index >= _nElements)
                return false;

            uint offset = _entryIndexSize switch
            {
                0 => MemoryHelper.ReadUnaligned<byte>(_base, _baseOffset + (int) (index / BlockSize)),
                1 => MemoryHelper.ReadUnaligned<ushort>(_base, _baseOffset + (int) (2 * (index / BlockSize))),
                _ => MemoryHelper.ReadUnaligned<uint>(_base, _baseOffset + (int) (4 * (index / BlockSize)))
            };

            offset += (uint) _baseOffset;

            for (uint bit = BlockSize >> 1; bit > 0; bit >>= 1)
            {
                uint val;
                uint offset2;

                unsafe
                {
                    offset2 = (uint) DecodeUnsigned((int) offset, &val);
                }

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

        public bool DisableReadyToRun(MethodBase method)
        {
            if (OSHelper.IsOSX)
                return false;

            int index = method.GetRID() - 1;

            uint offset = _entryIndexSize switch
            {
                0 => MemoryHelper.ReadUnaligned<byte>(_base, _baseOffset + (int) (index / BlockSize)),
                1 => MemoryHelper.ReadUnaligned<ushort>(_base, _baseOffset + (int) (2 * (index / BlockSize))),
                _ => MemoryHelper.ReadUnaligned<uint>(_base, _baseOffset + (int) (4 * (index / BlockSize)))
            };

            offset += (uint) _baseOffset;
            MemoryHelper.UnprotectWrite(_base, (int) offset, 0x00);

            return true;
        }
    }
}