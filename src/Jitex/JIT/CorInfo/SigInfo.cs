using System;
using System.Runtime.InteropServices;
using Jitex.Framework.Offsets;

namespace Jitex.JIT.CorInfo
{
    public class SigInfo : CorType
    {
        private ushort _numArgs;
        private IntPtr _args;
        private IntPtr _signature;

        private IntPtr NumArgsAddr => HInstance + SigInfoOffset.NumArgs;
        private IntPtr ArgsAddr => HInstance + SigInfoOffset.Args;
        private IntPtr SignatureAddr => HInstance + SigInfoOffset.Signature;

        public ushort NumArgs
        {
            get
            {
                if (_numArgs == 0)
                    _numArgs = (ushort)Marshal.ReadInt16(NumArgsAddr);

                return _numArgs;
            }
            set
            {
                _numArgs = value;
                Marshal.WriteInt16(NumArgsAddr, (short)_numArgs);
            }
        }

        public IntPtr Args
        {
            get
            {
                if (_args == IntPtr.Zero)
                    _args = Marshal.ReadIntPtr(ArgsAddr);

                return _args;
            }
            set
            {
                _args = value;
                Marshal.WriteIntPtr(ArgsAddr, _args);
            }
        }

        public IntPtr Signature
        {
            get
            {
                if (_signature == IntPtr.Zero)
                    _signature = Marshal.ReadIntPtr(SignatureAddr);

                return _signature;
            }
            set
            {
                _signature = value;
                Marshal.WriteIntPtr(SignatureAddr, _signature);
            }
        }

        public SigInfo(IntPtr hInstance) : base(hInstance){}
    }
}
