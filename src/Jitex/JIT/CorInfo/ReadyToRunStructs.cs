using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Jitex.JIT.CorInfo
{
    enum ReadyToRunSectionType
    {
        CompilerIdentifier = 100,
        ImportSections = 101,
        RuntimeFunctions = 102,
        MethodDefEntryPoints = 103,
        ExceptionInfo = 104,
        DebugInfo = 105,
        DelayLoadMethodCallThunks = 106,
        // 107 used by an older format of AvailableTypes
        AvailableTypes = 108,
        InstanceMethodEntryPoints = 109,
        InliningInfo = 110, // Added in V2.1, deprecated in 4.1
        ProfileDataInfo = 111, // Added in V2.2
        ManifestMetadata = 112, // Added in V2.3
        AttributePresence = 113, // Added in V3.1
        InliningInfo2 = 114, // Added in V4.1
        ComponentAssemblies = 115, // Added in V4.1
        OwnerCompositeExecutable = 116, // Added in V4.1
        PgoInstrumentationData = 117, // Added in V5.2
        ManifestAssemblyMvids = 118, // Added in V5.3
    };

    [StructLayout(LayoutKind.Sequential)]
    readonly struct READYTORUN_SECTION
    {
        public readonly ReadyToRunSectionType Type;           // READYTORUN_SECTION_XXX
        public readonly IMAGE_DATA_DIRECTORY Section;
    };

    [StructLayout(LayoutKind.Sequential)]
    readonly struct IMAGE_DATA_DIRECTORY
    {
        public readonly uint VirtualAddress;
        public readonly uint Size;
    }

    [StructLayout(LayoutKind.Sequential)]
    readonly struct READYTORUN_HEADER
    {
        public readonly uint Signature;      // READYTORUN_SIGNATURE
        public readonly ushort MajorVersion;   // READYTORUN_VERSION_XXX
        public readonly ushort MinorVersion;

        public readonly READYTORUN_CORE_HEADER CoreHeader;
    }

    [StructLayout(LayoutKind.Sequential)]
    readonly struct READYTORUN_CORE_HEADER
    {
        private readonly uint Flags;          // READYTORUN_FLAG_XXX

        public readonly uint NumberOfSections;
    };

    readonly struct READYTORUN_INFO
    {
        public readonly READYTORUN_HEADER Header;
        public readonly READYTORUN_SECTION Section;
        public readonly bool HasValue;

        public READYTORUN_INFO(READYTORUN_HEADER header, READYTORUN_SECTION section)
        {
            Header = header;
            Section = section;
            HasValue = true;
        }
    }
}
