# Resolvendo por Detour

O Jitex permite que você resolva um método desviando ele para outro método. O método destino pode ser um código gerenciado ou não.

Ao resolver um método por Detour, é adicionado um trampoline para o endereço de destino no início do código nativo no método de origem. 

```csharp
static void Main(string[] args)
{
    JitexManager.AddMethodResolver(MethodResolver);
    int result = Sum(10, 10);
    Console.WriteLine("Result from Sum "+result);
}

public static int Sum(int n1, int n2) => n1 + n2;
private static void MethodResolver(MethodContext context)
{
    if (context.Method.Name == "Sum")
    {
        context.ResolveDetour<Func<int, int, int>>((n1, n2) => n1 * n2);
    }
}
```

O resultado será:

```
Result from Sum: 100
```

No exemplo acima, o método Sum, será compilado e inserido o trampoline. Sendo assim, sempre que for executado, ele jogará o fluxo de execução para o delegate passado por parâmetro.

Além disso, é possível também fazer detour diretamente para métodos nativos. Basta apenas passar o endereço do método que deverá ser executado:

```csharp
public enum MemoryProtection
{
    EXECUTE_READ_WRITE = 0x40
}

[DllImport("kernel32", EntryPoint = "VirtualProtect")]
internal static extern int VirtualProtect(IntPtr lpAddress, long dwSize, MemoryProtection flNewProtect, out MemoryProtection lpflOldProtect);

private static IntPtr _nativeCodeAddress = IntPtr.Zero;
private static GCHandle _handle;

static void Main(string[] args)
{
    CreateNativeMethod();

    JitexManager.AddMethodResolver(MethodResolver);
    int result = Sum(10, 10);
    Console.WriteLine("Result from Sum "+result);
    _handle.Free();
}

/// <summary>
/// Create native code to mul 2 numbers.
/// </summary>
static void CreateNativeMethod()
{
    Assembler assembler = new Assembler(64);

    assembler.push(rbp);
    assembler.mov(rbp, rsp);
    assembler.mov(__[rbp + 0x10], ecx);
    assembler.mov(__[rbp + 0x18], edx);
    assembler.mov(eax, __[rbp + 0x10]);
    assembler.imul(eax, __[rbp + 0x18]);
    assembler.pop(rbp);
    assembler.ret();

    using MemoryStream ms = new MemoryStream();
    assembler.Assemble(new StreamCodeWriter(ms), 0);

    byte[] nativeCode = ms.ToArray();

    _handle = GCHandle.Alloc(nativeCode, GCHandleType.Pinned);
    _nativeCodeAddress = _handle.AddrOfPinnedObject();

    VirtualProtect(_nativeCodeAddress, nativeCode.Length, MemoryProtection.EXECUTE_READ_WRITE, out _);
}

public static int Sum(int n1, int n2) => n1 + n2;

private static void MethodResolver(MethodContext context)
{
    if (context.Method.Name == "Sum")
    {
        context.ResolveDetour(_nativeCodeAddress);
    }
}
```

O código acima apenas cria um código nativo para multiplicar 2 números.

Observe que nós removemos a proteção da memória do nosso código nativo usando VirtualProtect, isto porque o endereço informado para o Jitex, deve ser possível de ser executado pelo coreclr.

> Em sistemas operacionais POSIX (Linux e Mac), você pode utilizar o Mman.mmap para deixar o endereço acessível para o coreclr.

Executando o código acima, você terá o resultado:

```
Result from Sum: 100
```

