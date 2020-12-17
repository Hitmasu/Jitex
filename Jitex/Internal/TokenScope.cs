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

        public Module Module { get; set; }

        public int TokenReplace { get; set; }

        public TokenScope(int metadataToken, Module module, int tokenReplace)
        {
            MetadataToken = metadataToken;
            Module = module;
            TokenReplace = tokenReplace;
        }
    }
}
