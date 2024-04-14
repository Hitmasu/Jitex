using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Jitex.JIT.CorInfo;
using Jitex.Utils;
using Jitex.Utils.Extension;

namespace Jitex.JIT.Hooks.String;

/// <summary>
/// String resolver handler.
/// </summary>
/// <param name="context">Context form string.</param>
public delegate void StringResolverHandler(StringContext context);

internal class StringHook : HookBase<CEEInfo.ConstructStringLiteralDelegate>
{
    private static StringHook? Instance { get; set; }

    public StringHook() : base(Hook)
    {
    }

    public static StringHook GetInstance()
    {
        Instance ??= new StringHook();
        return Instance;
    }

    private static InfoAccessType Hook(IntPtr thisHandle, IntPtr hModule, int metadataToken,
        IntPtr ppValue)
    {
        if (thisHandle == IntPtr.Zero)
            return default;

        Tls ??= new ThreadTls();

        Tls.EnterCount++;

        try
        {
            if (Tls.EnterCount != 1)
                return CEEInfo.ConstructStringLiteral(thisHandle, hModule, metadataToken, ppValue);

            var resolvers = GetInvocationList<StringResolverHandler>();

            if (!resolvers.Any())
                return CEEInfo.ConstructStringLiteral(thisHandle, hModule, metadataToken, ppValue);

            var module = ModuleHelper.GetModuleByAddress(hModule);
            var content = module!.ResolveString(metadataToken);
            var context = new StringContext(module, metadataToken, content);

            foreach (var resolver in resolvers)
            {
                resolver(context);

                if (!context.IsResolved) continue;

                if (string.IsNullOrEmpty(context.Content))
                    throw new ArgumentNullException("String content can't be null or empty.");

                var result = CEEInfo.ConstructStringLiteral(thisHandle, hModule, metadataToken, ppValue);
                WriteString(ppValue, context.Content!);
                return result;
            }

            return CEEInfo.ConstructStringLiteral(thisHandle, hModule, metadataToken, ppValue);
        }
        finally
        {
            Tls.EnterCount--;
        }
    }

    /// <summary>
    /// Write string on OBJECTHANDLE.
    /// </summary>
    /// <param name="ppValue">Pointer to OBJECTHANDLE.</param>
    /// <param name="content">Content to write.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteString(IntPtr ppValue, string content)
    {
        IntPtr pEntry = Marshal.ReadIntPtr(ppValue);

        IntPtr objectHandle = Marshal.ReadIntPtr(pEntry);
        IntPtr hashMapPtr = Marshal.ReadIntPtr(objectHandle);
        byte[] newContent = Encoding.Unicode.GetBytes(content);

        objectHandle = Marshal.AllocHGlobal(IntPtr.Size + sizeof(int) + newContent.Length);

        Marshal.WriteIntPtr(objectHandle, hashMapPtr);
        Marshal.WriteInt32(objectHandle + IntPtr.Size, newContent.Length / 2);
        Marshal.Copy(newContent, 0, objectHandle + IntPtr.Size + sizeof(int), newContent.Length);

        Marshal.WriteIntPtr(pEntry, objectHandle);
    }
}