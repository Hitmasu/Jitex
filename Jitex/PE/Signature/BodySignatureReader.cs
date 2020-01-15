using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Jitex.PE.Signature
{
    internal class BodySignatureReader : SignatureReader
    {
        public IList<LocalVariableInfo> LocalVariables { get; set; }
        
        private byte[] _blobLocalVariables;
        private int _position;
        
        public BodySignatureReader(IntPtr address) : base(address)
        {
        }

        public override void Read()
        {
            Debug.Assert(Marshal.ReadByte(Address, 1) != 0x07,"Wrong LocalVarSig signature!");

            byte numberLocalVariables = Marshal.ReadByte(Address, 2);
            LocalVariables = new List<LocalVariableInfo>(numberLocalVariables);
            
            //Length - 2 explanation:
            //0x07 = Identifier LocalVarSig
            //0xXX = Total local variables
            //0xXX .. 0xXX = Total of bytes in signature
            _blobLocalVariables = new byte[Length - 2];
            
            Marshal.Copy(Address+3,_blobLocalVariables,0,_blobLocalVariables.Length);

            for (int i = 0; i < _blobLocalVariables.Length; i++)
            {
                Type type;
                byte flag = _blobLocalVariables[i];

                //Is primitive
                if (flag <= 0x0F)
                {
                    type = Enum.Parse<TypeInternal>(flag);
                }
            }
        }
        
        private Type GetType(byte flag)
        {
            TypeFlags typeFlag = (TypeFlags) flag;

            Type type;
            
            switch (typeFlag)
            {
                case TypeFlags.CategoryMask:
                    break;
                case TypeFlags.Unknown:
                case TypeFlags.Void:
                case TypeFlags.Boolean:
                case TypeFlags.Char:
                case TypeFlags.SByte:
                case TypeFlags.Byte:
                case TypeFlags.Int16:
                case TypeFlags.UInt16:
                case TypeFlags.Int32:
                case TypeFlags.UInt32:
                case TypeFlags.Int64:
                case TypeFlags.UInt64:
                case TypeFlags.IntPtr:
                case TypeFlags.UIntPtr:
                case TypeFlags.Single:
                case TypeFlags.Double:
                    type = ReadPrimitive();
                    break;
                case TypeFlags.ValueType:
                    break;
                case TypeFlags.Enum:
                    break;
                case TypeFlags.Nullable:
                    break;
                case TypeFlags.Class:
                    type = ReadClass();
                    break;
                case TypeFlags.Interface:
                    break;
                case TypeFlags.Array:
                    break;
                case TypeFlags.SzArray:
                    break;
                case TypeFlags.ByRef:
                    break;
                case TypeFlags.Pointer:
                    break;
                case TypeFlags.FunctionPointer:
                    break;
                case TypeFlags.GenericParameter:
                    break;
                case TypeFlags.SignatureTypeVariable:
                    break;
                case TypeFlags.SignatureMethodVariable:
                    break;
                case TypeFlags.HasGenericVariance:
                    break;
                case TypeFlags.HasGenericVarianceComputed:
                    break;
                case TypeFlags.HasStaticConstructor:
                    break;
                case TypeFlags.HasStaticConstructorComputed:
                    break;
                case TypeFlags.HasFinalizerComputed:
                    break;
                case TypeFlags.HasFinalizer:
                    break;
                case TypeFlags.IsByRefLike:
                    break;
                case TypeFlags.AttributeCacheComputed:
                    break;
                case TypeFlags.IsIntrinsic:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private Type ReadPrimitive()
        {
            TypeFlags typeFlag = (TypeFlags) _blobLocalVariables[_position++];
            return Type.GetType(typeFlag.ToString());
        }

        private Type ReadClass()
        {
            byte flag = _blobLocalVariables[_position++];
            byte type = _blobLocalVariables[_position++];
            
            BitArray bits = new BitArray(flag);
        }
    }
}