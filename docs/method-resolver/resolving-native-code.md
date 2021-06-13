## Resolvendo por código nativo

É possível efetuar a resolução do método através de código nativo. O MethodContext possui o método ResolveNative, que recebe um array de bytes que substituíra o método após a compilação:

```csharp
static void Main(string[] args)
{
    JitexManager.AddMethodResolver(MethodResolver);
    int result = Sum(10,10);
    Console.WriteLine(result);
}

public static int Sum(int n1, int n2) => n1 + n2;

private static void MethodResolver(MethodContext context)
{
    if (context.Method.Name == "Sum")
    {
        Assembler assembler = new Assembler(64);

        assembler.push(rbp);
        assembler.mov(rbp, rsp);
        assembler.mov(__[rbp + 0x10], ecx);
        assembler.mov(__[rbp + 0x18], edx);
        assembler.mov(eax, __[rbp+0x10]);
        assembler.add(eax, __[rbp+0x18]);
        assembler.pop(rbp);
        assembler.ret();
        
        using MemoryStream ms = new MemoryStream();
        assembler.Assemble(new StreamCodeWriter(ms), 0);

        byte[] nativeCode = ms.ToArray();

        context.ResolveNative(nativeCode);
    }
}
```

> Foi utilizado a biblioteca [Iced](https://github.com/icedland/iced) para facilitar a escrita do assembler.

Na implementação acima, substituímos o código nativo gerado pelo método pelo código nativo informado.

Para tornar esta substituição de código nativo possível, antes da compilação do método, o Jitex substitui o corpo original do método (MSIL) por várias instruções que quando compiladas, vão gerar um código nativo maior ou igual informado pelo desenvolvedor, tornado possível a substituição. Sem esta alteração no corpo do método, o desenvolvedor iria ficar limitado há um código nativo menor ou igual gerado originalmente pelo método.

O processo de substituição de instruções do corpo método não é tão otimizada. Por exemplo: se o desenvolvedor informar um código nativo 100 bytes, o código nativo compilado para substituição será de 147 bytes. Ao substituir o código nativo gerado (147 bytes) pelo informado (100 bytes), haverá uma perca de 47 bytes não utilizados na memória.

Em alguns cenários, esta perca de memória pode ser ignorada, porém, se espaço em memória é crítico no projeto, considere aplicar um detour ao invés de resolver o método por código nativo.

