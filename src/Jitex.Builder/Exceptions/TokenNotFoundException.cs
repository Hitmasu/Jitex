using System;
using System.Reflection;

namespace Jitex.Builder.Exceptions;

public class TokenNotFoundException : Exception
{
    public TokenNotFoundException(int metadataToken, Exception innerException) : base($"Token 0x{metadataToken:X} not found.", innerException)
    {
    }
}