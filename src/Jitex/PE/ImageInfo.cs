﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Jitex.Intercept;
using Jitex.Internal;
using Jitex.Utils;

namespace Jitex.PE
{
    internal sealed class ImageInfo
    {
        private readonly IDictionary<MethodBase, int> _memberRefs = new Dictionary<MethodBase, int>();
        private readonly IDictionary<MethodInfo, int> _methodsSpecs = new Dictionary<MethodInfo, int>();
        private readonly IDictionary<Type, int> _typeRefs = new Dictionary<Type, int>();
        public int NumberOfMemberRefRows { get; internal set; }

        public int NumberOfTypeRefRows { get; internal set; }
        public int NumberOfMethodSpecRows { get; internal set; }

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

        public void AddMemberRef(MethodBase method, out int token)
        {
            //if (method is MethodInfo {IsGenericMethod: true} methodInfo)
            //{
            //    AddMethodSpec(methodInfo, out token);
            //    return;
            //}

            if (_memberRefs.TryGetValue(method, out token))
                return;

            const int memberRefBase = 0x0A000000;
            token = memberRefBase + NumberOfMemberRefRows + 1;
            NumberOfMemberRefRows++;

            _memberRefs.Add(method, token);
            AddMemberToResolution(method, token);
        }

        public void AddTypeRef(Type type, out int token)
        {
            if (_typeRefs.TryGetValue(type, out token))
                return;

            const int typeRefBase = 0x01000000;
            token = typeRefBase + NumberOfTypeRefRows + 1;
            NumberOfTypeRefRows++;

            _typeRefs.Add(type, token);
            AddMemberToResolution(type, token);
        }

        public void AddMethodSpec(MethodInfo method, out int token)
        {
            if (_methodsSpecs.TryGetValue(method, out token))
                return;

            const int specRefBase = 0x2B000000;
            token = specRefBase + NumberOfMethodSpecRows + 1;
            NumberOfMethodSpecRows++;

            _methodsSpecs.Add(method, token);
            AddMemberToResolution(method, token);
        }

        /// <summary>
        /// Add a new MemberInfo to resolution on internal module.
        /// </summary>
        /// <param name="memberInfo">MemberInfo resolution.</param>
        /// <param name="token">Token to resolve.</param>
        private void AddMemberToResolution(MemberInfo memberInfo, int token)
        {
            if (!JitexManager.ModuleIsLoaded<InternalModule>())
                JitexManager.LoadModule(InternalModule.Instance);

            InternalModule.Instance.AddMemberToResolution(Module, token, memberInfo);
        }

        internal Type? GetTypeRef(int refToken) => _typeRefs.FirstOrDefault(w => w.Value == refToken).Key;

        internal void AddMemberRefFromImage(MethodBase methodBase, int refToken) => _memberRefs.Add(methodBase, refToken);
        internal void AddTypeRefFromImage(Type type, int refToken) => _typeRefs.Add(type, refToken);
        internal void AddMethodSpecFromImage(MethodInfo methodInfo, int refToken) => _methodsSpecs.Add(methodInfo, refToken);
    }
}