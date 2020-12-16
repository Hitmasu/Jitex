using System;
using System.Reflection;

namespace Jitex.Internal
{
    internal class TokenScope
    {
        /// <summary>
        /// Token to resolve.
        /// </summary>
        public int MetadataToken { get; }

        public IntPtr Scope  { get; }

        public TokenScope(int metadataToken, IntPtr scope)
        {
            MetadataToken = metadataToken;
            Scope = scope;  
        }
    }
}
