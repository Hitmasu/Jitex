using Jitex.Utils.Comparer;
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
        private readonly Module _module;

        private ImmutableDictionary<Type, EntityHandle> Types { get; }
        private ImmutableDictionary<int, int> MembersRef { get; }

        /// <summary>
        /// Read metadata from assembly.
        /// </summary>
        /// <param name="assembly">Assembly to read.</param>
        public MetadataInfo(Assembly assembly)
        {
            _module = assembly.ManifestModule;

            using Stream assemblyFile = File.OpenRead(assembly.Location);
            using PEReader peReader = new PEReader(assemblyFile);
            MetadataReader metadataReader = peReader.GetMetadataReader();

            Types = ReadTypes(metadataReader);
            MembersRef = ReadReferences(metadataReader);
        }

        /// <summary>
        /// Read types from metadata.
        /// </summary>
        /// <param name="reader">Instance of MetadataReader</param>
        /// <returns>A Dictionary of types found.</returns>
        private ImmutableDictionary<Type, EntityHandle> ReadTypes(MetadataReader reader)
        {
            var types = ImmutableDictionary.CreateBuilder<Type, EntityHandle>(TypeComparer.Instance);

            IEnumerable<EntityHandle> typesDef = reader.TypeDefinitions.Select(typeDef => (EntityHandle)typeDef);
            IEnumerable<EntityHandle> typesRef = reader.TypeReferences.Select(typeRef => (EntityHandle)typeRef); ;

            foreach (EntityHandle entityHandle in typesDef.Concat(typesRef))
            {
                int token = reader.GetToken(entityHandle);

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

        private ImmutableDictionary<int, int> ReadReferences(MetadataReader reader)
        {
            var references = ImmutableDictionary.CreateBuilder<int, int>();

            foreach (int tokenRef in reader.MemberReferences.Select(reference => reader.GetToken(reference)))
            {
                MemberInfo memberRef = _module.ResolveMember(tokenRef);
                references.Add(memberRef.MetadataToken, tokenRef);
            }

            return references.ToImmutableDictionary();
        }

        /// <summary>
        /// Get handle from Type.
        /// </summary>
        /// <param name="type">Type to get handle.</param>
        /// <returns>EntityHandle from Type.</returns>
        internal EntityHandle GetTypeHandle(Type type)
        {
            if (Types.TryGetValue(type, out EntityHandle typeInfo))
                return typeInfo;

            throw new NullReferenceException("Type not referenced on assembly.");
        }

        public int GetMemberRefToken(int metadataToken)
        {
            if (MembersRef.TryGetValue(metadataToken, out int memberRefToken))
                return memberRefToken;

            throw new NullReferenceException("Type not referenced on assembly.");
        }
    }
}