using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Jitex.Intercept;
using Jitex.Utils;

namespace Jitex.PE
{
    internal class ImageInfo
    {
        private readonly IDictionary<MethodBase, int> MethodRefs = new Dictionary<MethodBase, int>();
        private readonly IDictionary<Type, int> TypeRefs = new Dictionary<Type, int>();

        public int MethodRefRows { get; internal set; }
        public int TypeRefRows { get; internal set; }
        public Module Module { get; internal set; }
        public IntPtr BaseAddress { get; internal set; }
        public int Size { get; internal set; }
        public uint NumberOfElements { get; internal set; }
        public byte EntryIndexSize { get; internal set; }
        public int BaseOffset { get; internal set; }

        internal ImageInfo(Module module)
        {
            Module = module;
        }

        internal ImageInfo(Module module, IntPtr baseAddress, int size, int baseOffset, uint numberOfElements, byte entryIndexSize)
        {
            Module = module;
            BaseAddress = baseAddress;
            Size = size;
            NumberOfElements = numberOfElements;
            EntryIndexSize = entryIndexSize;
            BaseOffset = baseOffset;
        }

        /// <summary>
        /// Returns
        /// </summary>
        /// <param name="method"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public void AddMethodRef(MethodBase method, out int token)
        {
            if (MethodRefs.TryGetValue(method, out token))
                return;

            const int methodRefBase = 0x0A000000;
            int newMethodRefToken = methodRefBase + MethodRefs.Count + 1;
            token = newMethodRefToken;

            AddTokenToResolution(method, token);
        }

        public void AddTypeRef(Type type, out int token)
        {
            if (TypeRefs.TryGetValue(type, out token))
                return;

            const int typeRefBase = 0x01000000;
            int newTypeRefToken = typeRefBase + TypeRefs.Count + 1;
            token = newTypeRefToken;

            AddTokenToResolution(type, token);
        }

        internal void AddNewMethodRef(MethodBase methodBase, int refToken) => MethodRefs.Add(methodBase, refToken);
        internal void AddNewTypeRef(Type type, int refToken) => TypeRefs.Add(type, refToken);

        private void AddTokenToResolution(MemberInfo memberInfo, int token)
        {
            InternalModule.Instance.AddTokenToResolution(Module, token, memberInfo);
        }
    }
}