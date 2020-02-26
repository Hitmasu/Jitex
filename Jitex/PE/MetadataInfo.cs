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

namespace Jitex.PE
{
    /// <summary>
    /// Read Metadata from assembly.
    /// </summary>
    public class MetadataInfo
    {
        private readonly Assembly _assembly;

        /// <summary>
        /// Types from Assembly. Key is the Metadata Token from Type.
        /// </summary>
        private ImmutableDictionary<int, EntityHandle> Types { get; }

        /// <summary>
        /// Read metadata from assembly.
        /// </summary>
        /// <param name="assembly">Assembly to read.</param>
        public MetadataInfo(Assembly assembly)
        {
            _assembly = assembly;
            using Stream assemblyFile = File.OpenRead(assembly.Location);
            using PEReader peReader = new PEReader(assemblyFile);
            MetadataReader metadataReader = peReader.GetMetadataReader();

            Types = ReadTypes(metadataReader);
        }

        /// <summary>
        /// Read types from metadata.
        /// </summary>
        /// <param name="reader">Instance of MetadataReader</param>
        /// <returns>A Dictionary of types found.</returns>
        private ImmutableDictionary<int, EntityHandle> ReadTypes(MetadataReader reader)
        {
            var types = ImmutableDictionary.CreateBuilder<int, EntityHandle>();

            IEnumerable<EntityHandle> typesDef = reader.TypeDefinitions.Select(typeDef => (EntityHandle)typeDef);
            IEnumerable<EntityHandle> typesRef = reader.TypeReferences.Select(typeRef => (EntityHandle)typeRef); ;

            foreach (EntityHandle entityHandle in typesDef.Concat(typesRef))
            {
                int token = reader.GetToken(entityHandle);

                Type type = null;

                foreach (Module module in _assembly.Modules)
                {
                    try
                    {
                        type = module.ResolveType(token);
                        break;
                    }
                    catch
                    {
                        //ignore;
                    }
                }

                if (type == null && token != 0x02000001)
                    throw new NullReferenceException("Type not referenced on assembly.");

                types.Add(token, entityHandle);
            }

            return types.ToImmutableDictionary();
        }

        /// <summary>
        /// Get info from Type.
        /// </summary>
        /// <param name="type">Type to get info.</param>
        /// <returns>A TypeInfo from type.</returns>
        internal EntityHandle GetTypeHandle(Type type)
        {
            if (Types.TryGetValue(type.MetadataToken, out EntityHandle typeInfo))
                return typeInfo;

            throw new NullReferenceException("Type not referenced on assembly.");
        }
    }
}