﻿using System;

namespace Jitex.PE.Signature
{
    public class LocalVariableInfo
    {
        public Type Type { get; }
        public CorElementType ElementType => GetElementType();

        public LocalVariableInfo(Type type)
        {
            Type = type;
        }
        
        private CorElementType GetElementType()
        {
            return (CorElementType)(int)Type.GetTypeCode(Type);
        }
    }
}