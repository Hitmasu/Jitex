﻿using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Jitex.Builder.Exceptions;
using Jitex.PE.Signature;
using LocalVariableInfo = Jitex.PE.Signature.LocalVariableInfo;

namespace Jitex.Builder
{
    public class MethodBodyBuilder
    {
        public Module Module { get; set; }
        public byte[] IL { get; set; }
        public IList<LocalVariableInfo> LocalVariables { get; set; }

        public MethodBodyBuilder(byte[] il, IList<LocalVariableInfo> localVariables, Module module)
        {
            IL = il;
            LocalVariables = localVariables;
        }

        public MethodBodyBuilder(byte[] il)
        {
            IL = il;
            LocalVariables = new List<LocalVariableInfo>();
        }

        private byte[] GetSignature()
        {
            IList<int> signature = new List<int>();
            signature.Add(LocalVariables.Count);
            signature.Add(0x07);
            signature.Add(LocalVariables.Count);

            foreach (LocalVariableInfo localVariable in LocalVariables)
            {
                 CorElementType elementType = localVariable.ElementType;

                 if (elementType == CorElementType.ELEMENT_TYPE_CLASS)
                 {
                     if (Module == null)
                         throw new ModuleNullException("Local variable Class cannot be null.");
                 }
            }
            
            signature.Insert(0,signature.Count);
            return signature.Cast<byte>().ToArray();
        }
    }
}