using System;
using System.Reflection;
using System.Runtime.InteropServices;
using Jitex.Runtime.Offsets;
using Jitex.Utils;

namespace Jitex.JIT.CorInfo
{
    internal class ResolvedToken
    {
        private readonly IntPtr _hInstance;

        private IntPtr _context;
        private Module? _module;
        private int _token;
        private TokenKind? _type;
        private IntPtr _hClass;
        private IntPtr _hMethod;
        private IntPtr _hField;

        private IntPtr ContextAddr => _hInstance + ResolvedTokenOffset.Context;
        private IntPtr ModuleAddr => _hInstance + ResolvedTokenOffset.Module;
        private IntPtr TokenAddr => _hInstance + ResolvedTokenOffset.Token;
        private IntPtr TypeAddr => _hInstance + ResolvedTokenOffset.Type;
        private IntPtr HClassAddr => _hInstance + ResolvedTokenOffset.HClass;
        private IntPtr HMethodAddr => _hInstance + ResolvedTokenOffset.HMethod;
        private IntPtr HFieldAddr => _hInstance + ResolvedTokenOffset.HField;

        public IntPtr Context
        {
            get
            {
                if (_context == IntPtr.Zero)
                    _context = Marshal.ReadIntPtr(ContextAddr);

                return _context;
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
            set
            {
                _module = value;
                IntPtr scope = AppModules.GetAddressFromModule(_module);
                Marshal.WriteIntPtr(ModuleAddr, scope);
            }
        }

        public int Token
        {
            get
            {
                if (_token == 0)
                    _token = Marshal.ReadInt32(TokenAddr);

                return _token;
            }
            set
            {
                _token = value;
                Marshal.WriteInt32(TokenAddr, _token);
            }
        }

        public TokenKind Type
        {
            get
            {
                _type ??= (TokenKind)Marshal.ReadInt32(TypeAddr);
                return _type.Value;
            }
        }

        public IntPtr HClass
        {
            get
            {
                if (_hClass == IntPtr.Zero)
                    _hClass = Marshal.ReadIntPtr(HClassAddr);
                return _hClass;
            }
            set
            {
                _hClass = value;
                Marshal.WriteIntPtr(HClassAddr, _hClass);
            }
        }

        public IntPtr HMethod
        {
            get
            {
                if (_hMethod == IntPtr.Zero)
                    _hMethod = Marshal.ReadIntPtr(HMethodAddr);
                return _hMethod;
            }
            set
            {
                _hMethod = value;
                Marshal.WriteIntPtr(HMethodAddr, _hMethod);
            }
        }

        public IntPtr HField
        {
            get
            {
                if (_hField == IntPtr.Zero)
                    _hField = Marshal.ReadIntPtr(HFieldAddr);
                return _hField;
            }
            set
            {
                _hField = value;
                Marshal.WriteIntPtr(HFieldAddr, _hField);
            }
        }

        public ResolvedToken(IntPtr hInstance)
        {
            _hInstance = hInstance;
        }
    }
}
