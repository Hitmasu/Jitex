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

internal class StringHook : HookBase
{
    private static CEEInfo.ConstructStringLiteralDelegate DelegateHook;

    [ThreadStatic]
    private static ThreadTls? Tls;

    private static StringHook? Instance { get; set; }

    public static StringHook GetInstance()
    {
        Instance ??= new StringHook();
        return Instance;
    }

    private InfoAccessType Hook(IntPtr thisHandle, IntPtr hModule, int metadataToken,
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

            foreach (StringResolverHandler resolver in resolvers)
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
        var pEntry = Marshal.ReadIntPtr(ppValue);

        var objectHandle = Marshal.ReadIntPtr(pEntry);
        var hashMapPtr = Marshal.ReadIntPtr(objectHandle);
        var newContent = Encoding.Unicode.GetBytes(content);

        objectHandle = Marshal.AllocHGlobal(IntPtr.Size + sizeof(int) + newContent.Length);

        Marshal.WriteIntPtr(objectHandle, hashMapPtr);
        Marshal.WriteInt32(objectHandle + IntPtr.Size, newContent.Length / 2);
        Marshal.Copy(newContent, 0, objectHandle + IntPtr.Size + sizeof(int), newContent.Length);

        Marshal.WriteIntPtr(pEntry, objectHandle);
    }

    public override void PrepareHook()
    {
        DelegateHook = Hook;
        HookAddress = Marshal.GetFunctionPointerForDelegate(DelegateHook);
        RuntimeHelperExtension.PrepareDelegate(DelegateHook, IntPtr.Zero, IntPtr.Zero, 0, IntPtr.Zero);
    }
}