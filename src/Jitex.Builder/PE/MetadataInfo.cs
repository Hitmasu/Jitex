using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using Jitex.Builder.Utils.Comparer;
using Lokad.ILPack;

namespace Jitex.Builder.PE
{
    /// <summary>
    ///     Read Metadata from assembly.
    /// </summary>
    public class MetadataInfo
    {
        private readonly Module _module;

        private ImmutableDictionary<Type, EntityHandle> Types { get; }

        public Assembly Assembly { get; }

        /// <summary>
        ///     Read metadata from assembly.
        /// </summary>
        /// <param name="assembly">Assembly to read.</param>
        public MetadataInfo(Assembly assembly)
        {
            _module = assembly.ManifestModule;
            Assembly = assembly;

            Stream assemblyStream;

            if (assembly.IsDynamic)
            {
                AssemblyGenerator generator = new AssemblyGenerator();
                byte[] buffer = generator.GenerateAssemblyBytes(assembly);
                assemblyStream = new MemoryStream(buffer);
            }
            else
            {
                assemblyStream = File.OpenRead(assembly.Location);
            }

            using PEReader peReader = new PEReader(assemblyStream);

            var lp = peReader.PEHeaders.PEHeader.AddressOfEntryPoint;
            MetadataReader metadataReader = peReader.GetMetadataReader();

            Types = ReadTypes(metadataReader);

            assemblyStream.Dispose();
        }

        unsafe uint DecodeUnsigned(uint offset, uint* pValue)
        {
            int* _base = default;
            uint val = (uint)*(_base + offset);
            if ((val & 1) == 0)
            {
                *pValue = (val >> 1);
                offset += 1;
            }
            else
            if ((val & 2) == 0)
            {
                *pValue = (val >> 2) |
                          (((uint)*(_base + offset + 1)) << 6);
                offset += 2;
            }
            else
            if ((val & 4) == 0)
            {
                *pValue = (val >> 3) |
                          (((uint)*(_base + offset + 1)) << 5) |
                          (((uint)*(_base + offset + 2)) << 13);
                offset += 3;
            }
            else
            if ((val & 8) == 0)
            {
                *pValue = (val >> 4) |
                          (((uint)*(_base + offset + 1)) << 4) |
                          (((uint)*(_base + offset + 2)) << 12) |
                          (((uint)*(_base + offset + 3)) << 20);
                offset += 4;
            }
            else
            if ((val & 16) == 0)
            {
                //*pValue = ReadUInt32(offset + 1);
                offset += 5;
            }
            else
            {
                Debugger.Break();
            }

            return offset;
        }

        /// <summary>
        ///     Get handle from Type.
        /// </summary>
        /// <param name="type">Type to get handle.</param>
        /// <returns>EntityHandle from Type or default if not found on assembly.</returns>
        internal EntityHandle GetTypeHandle(Type type)
        {
            return Types.TryGetValue(type, out EntityHandle typeInfo) ? typeInfo : default;
        }

        /// <summary>
        ///     Read types from metadata.
        /// </summary>
        /// <param name="reader">Instance of MetadataReader</param>
        /// <returns>A Dictionary of types found on metadata..</returns>
        private ImmutableDictionary<Type, EntityHandle> ReadTypes(MetadataReader reader)
        {
            var types = ImmutableDictionary.CreateBuilder<Type, EntityHandle>(TypeComparer.Instance);

            IEnumerable<EntityHandle> typesDef = reader.TypeDefinitions.Select(typeDef => (EntityHandle)typeDef);
            IEnumerable<EntityHandle> typesRef = reader.TypeReferences.Select(typeRef => (EntityHandle)typeRef);

            foreach (EntityHandle entityHandle in typesDef.Concat(typesRef))
            {
                int token = reader.GetToken(entityHandle);

                if (token == 0x02000001)
                    continue;

                Type type = null;

                try
                {
                    type = _module.ResolveType(token);
                }
                catch
                {
                    //ignore;
                }

                if (type != null)
                    types.Add(type, entityHandle);
            }

            return types.ToImmutableDictionary();
        }
    }
}