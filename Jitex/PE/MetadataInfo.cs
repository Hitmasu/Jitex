using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using Jitex.Tools;

namespace Jitex.PE
{
    public class MetadataInfo
    {
        private readonly Assembly _assembly;

        internal ImmutableDictionary<int, TypeInfo> Types { get; }

        public MetadataInfo(Assembly assembly)
        {
            _assembly = assembly;
            using Stream assemblyFile = File.OpenRead(assembly.Location);
            using PEReader peReader = new PEReader(assemblyFile);
            MetadataReader metadataReader = peReader.GetMetadataReader();

            Types = ReadTypes(metadataReader);
        }

        private ImmutableDictionary<int, TypeInfo> ReadTypes(MetadataReader reader)
        {
            var types = ImmutableDictionary.CreateBuilder<int, TypeInfo>();

            IEnumerable<EntityHandle> typesDef = reader.TypeDefinitions.Select(typeDef => (EntityHandle)typeDef);
            IEnumerable<EntityHandle> typesRef = reader.TypeReferences.Select(typeRef => (EntityHandle)typeRef); ;

            foreach (EntityHandle entityHandle in typesDef.Concat(typesRef))
            {
                int rowNumber = reader.GetRowNumber(entityHandle);
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

                Debug.Assert(type != null || token == 0x02000001, "Type can't be null");

                if (type != null)
                {
                    TypeIdentifier typeIdentifier = entityHandle.Kind == HandleKind.TypeDefinition ? TypeIdentifier.TypeDef : TypeIdentifier.TypeRef;

                    TypeInfo typeInfo = new TypeInfo(type, rowNumber, typeIdentifier);
                    types.Add(token, typeInfo);
                }
            }

            return types.ToImmutableDictionary();
        }

        private int GetRowNumber<T>()
        {
            if (Types.TryGetValue(typeof(T).MetadataToken, out TypeInfo type))
            {
                return type.RowNumber;
            }

            Debug.Assert(type != null, "Type not found!");

            return -1;
        }
    }
}