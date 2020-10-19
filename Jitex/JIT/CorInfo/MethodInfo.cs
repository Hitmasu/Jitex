using System;
using System.Reflection;
using System.Runtime.InteropServices;
using Jitex.Runtime;
using Jitex.Utils;

namespace Jitex.JIT.CorInfo
{
    internal class MethodInfo
    {
        private readonly IntPtr _hInstance;

        private IntPtr _methodDesc;
        private Module _module;
        private IntPtr _ilCode;
        private uint _ilCodeSize;
        private uint _maxStack;


        private IntPtr MethodDescAddr => _hInstance + MethodInfoOffset.MethodDesc;
        private IntPtr ModuleAddr => _hInstance + MethodInfoOffset.Module;
        private IntPtr ILCodeAddr => _hInstance + MethodInfoOffset.ILCode;
        private IntPtr ILCodeSizeAddr => _hInstance + MethodInfoOffset.ILCodeSize;
        private IntPtr MaxStackAddr => _hInstance + MethodInfoOffset.MaxStack;

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
                    _module = AppModules.GetModuleByAddress(scope);
                }

                return _module;
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

        public MethodInfo(IntPtr hInstance)
        {
            _hInstance = hInstance;
            IntPtr sigIntance = _hInstance + MethodInfoOffset.Locals;
            Locals = new SigInfo(sigIntance);
        }
    }
}
