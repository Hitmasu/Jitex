using System;
using System.Reflection;
using System.Runtime.InteropServices;
using Jitex.Framework.Offsets;
using Jitex.Utils;

namespace Jitex.JIT.CorInfo
{
    internal class ResolvedToken : CorType
    {
        private IntPtr _context;
        private int _token;
        private IntPtr _scope = IntPtr.Zero;
        private TokenKind? _type;
        private IntPtr _hClass;
        private IntPtr _hMethod;
        private IntPtr _hField;

        private IntPtr ContextAddr => HInstance + ResolvedTokenOffset.Context;
        private IntPtr ScopeAddr => HInstance + ResolvedTokenOffset.Scope;
        private IntPtr TokenAddr => HInstance + ResolvedTokenOffset.Token;
        private IntPtr TypeAddr => HInstance + ResolvedTokenOffset.Type;
        private IntPtr HClassAddr => HInstance + ResolvedTokenOffset.HClass;
        private IntPtr HMethodAddr => HInstance + ResolvedTokenOffset.HMethod;
        private IntPtr HFieldAddr => HInstance + ResolvedTokenOffset.HField;

        public IntPtr Context
        {
            get
            {
                if (_context == IntPtr.Zero)
                    _context = Marshal.ReadIntPtr(ContextAddr);

                return _context;
            }
            set
            {
                _context = value;
                Marshal.WriteIntPtr(ContextAddr, _context);
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
            get => ModuleHelper.GetModuleByAddress(Scope);

            set
            {
                if(value == null)
                    throw new ArgumentNullException();

                IntPtr moduleAddress = ModuleHelper.GetAddressFromModule(value);
                Scope = moduleAddress;
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
                _type ??= (TokenKind) Marshal.ReadInt32(TypeAddr);
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

        public ResolvedToken(IntPtr hInstance) : base(hInstance)
        {
        }
    }
}