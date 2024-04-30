using System.Reflection;

namespace Jitex.JIT.Hooks.String;

public class StringContext : Contextbase
{
    public Module Module { get; private set; }
    public int MetadataToken { get; private set; }
    public string Content { get; private set; }

    public StringContext(Module module, int metadataToken, string content) : base(null, false)
    {
        Module = module;
        MetadataToken = metadataToken;
        Content = content;
    }

    public void ResolveString(string newContent)
    {
        IsResolved = true;
        Content = newContent;
    }
}