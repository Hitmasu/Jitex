using System;
using System.Reflection;
using System.Runtime.InteropServices;
using Jitex.Runtime;
using Jitex.Utils;

namespace Jitex.JIT.CorInfo
{
    internal class MethodInfo : CorType
    {
        private IntPtr _methodDesc;
        private Module _module = null!;
        private IntPtr _ilCode;
        private uint _ilCodeSize;
        private uint _maxStack;
        private uint _ehCount;

        private IntPtr MethodDescAddr => HInstance + MethodInfoOffset.MethodDesc;
        private IntPtr ModuleAddr => HInstance + MethodInfoOffset.Module;
        private IntPtr ILCodeAddr => HInstance + MethodInfoOffset.ILCode;
        private IntPtr ILCodeSizeAddr => HInstance + MethodInfoOffset.ILCodeSize;
        private IntPtr MaxStackAddr => HInstance + MethodInfoOffset.MaxStack;
        private IntPtr EHCountAddr => HInstance + MethodInfoOffset.EHCount;

        public SigInfo Locals { get; set; }

        public IntPtr MethodDesc
        {
            get
            {
                if (_methodDesc == IntPtr.Zero)
                    _methodDesc = Marshal.ReadIntPtr(MethodDescAddr);

                return _methodDesc;
            }
        }

        public Module Module
        {
            get
            {
                if (_module == null)
                {
                    IntPtr scope = Marshal.ReadIntPtr(ModuleAddr);
                    _module = AppModules.GetModuleByAddress(scope)!;
                }

                return _module;
            }
            set
            {
                _module = value;
                IntPtr scope = AppModules.GetAddressFromModule(_module);
                Marshal.WriteIntPtr(ModuleAddr, scope);
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
