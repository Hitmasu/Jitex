﻿using System;
using System.Reflection;
using System.Runtime.InteropServices;
using Jitex.Framework.Offsets;
using Jitex.Utils;

namespace Jitex.JIT.CorInfo
{
    internal class MethodInfo : CorType
    {
        private IntPtr _methodDesc;
        private IntPtr _scope = IntPtr.Zero;
        private IntPtr _ilCode;
        private uint _ilCodeSize;
        private uint _maxStack;
        private uint _ehCount;

        private IntPtr MethodDescAddr => HInstance + MethodInfoOffset.MethodDesc;
        private IntPtr ScopeAddr => HInstance + MethodInfoOffset.Scope;
        private IntPtr ILCodeAddr => HInstance + MethodInfoOffset.ILCode;
        private IntPtr ILCodeSizeAddr => HInstance + MethodInfoOffset.ILCodeSize;
        private IntPtr MaxStackAddr => HInstance + MethodInfoOffset.MaxStack;
        private IntPtr EHCountAddr => HInstance + MethodInfoOffset.EHCount;

        public SigInfo Locals { get; }

        public IntPtr MethodHandle
        {
            get
            {
                if (_methodDesc == IntPtr.Zero)
                    _methodDesc = Marshal.ReadIntPtr(MethodDescAddr);

                return _methodDesc;
            }
        }

        public IntPtr Scope
        {
            get
            {
                if (_scope == default)
                    _scope = Marshal.ReadIntPtr(ScopeAddr);

                return _scope;
            }
            set
            {
                _scope = value;
                Marshal.WriteIntPtr(ScopeAddr, _scope);
            }
        }

        public Module? Module
        {
            get => AppModules.GetModuleByHandle(_scope);

            set
            {
                if(value == null)
                    throw new ArgumentNullException();

                IntPtr scope = AppModules.GetAddressFromModule(value);
                Scope = scope;
            }
        }

        public IntPtr ILCode
        {
            get
            {
                if (_ilCode == IntPtr.Zero)
                {
                    _ilCode = Marshal.ReadIntPtr(ILCodeAddr);
                }

                return _ilCode;
            }
            set
            {
                _ilCode = value;
                Marshal.WriteIntPtr(ILCodeAddr, _ilCode);
            }
        }

        public uint ILCodeSize
        {
            get
            {
                if (_ilCodeSize == 0)
                {
                    _ilCodeSize = (uint)Marshal.ReadInt32(ILCodeSizeAddr);
                }

                return _ilCodeSize;
            }
            set
            {
                _ilCodeSize = value;
                Marshal.WriteInt32(ILCodeSizeAddr, (int)_ilCodeSize);
            }
        }

        public uint MaxStack
        {
            get
            {
                if (_maxStack == 0)
                {
                    _maxStack = (uint)Marshal.ReadInt32(MaxStackAddr);
                }

                return _maxStack;
            }
            set
            {
                _maxStack = value;
                Marshal.WriteInt32(MaxStackAddr, (int)_maxStack);
            }
        }

        public uint EHCount
        {
            get
            {
                if (_ehCount == 0)
                {
                    _ehCount = (uint)Marshal.ReadInt32(EHCountAddr);
                }

                return _ehCount;
            }
            set
            {
                _ehCount = value;
                Marshal.WriteInt32(EHCountAddr, (int)_ehCount);
            }
        }

        public MethodInfo(IntPtr hInstance) : base(hInstance)
        {
            IntPtr sigIntance = HInstance + MethodInfoOffset.Locals;
            Locals = new SigInfo(sigIntance);
        }
    }
}
