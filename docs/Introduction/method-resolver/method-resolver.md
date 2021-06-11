## Resolução de Métodos

O Jitex permite que você mude o corpo de um método antes de ser compilado pelo JIT durante a execução da aplicação. Desta forma, é possível criar fluxos que só são de conhecimento em tempo de execução sem grandes modificações no código original.

Para fazer isto, é exposto um resolver chamado MethodResolver, em que todos os métodos antes de serem compilados pelo JIT (e enquanto o Jitex estiver ativo), são enviados à ele para que o desenvolvedor defina como deve ser compilado.

Para adicionar um resolver, basta apenas chamar o método AddMethodResolver da JitexManager passando o sue resolver:

```csharp
static void Main(string[] args)
{
    JitexManager.AddMethodResolver(context =>
    {
        if (context.Method.Name == "Sum")
            Console.WriteLine("Method to compile: " + context.Method.ToString());
    });

    Console.WriteLine("First random number: " + Sum(1, 1));
    Console.WriteLine("Second random number: " + Sum(2, 2));
}

static int Sum(int n1, int n2) => n1 + n2;
```

No exemplo acima, o resultado será:

```
Method to compile: Int32 Sum(Int32, Int32)
First random number: 2
Second random number: 4
```

Observe que o método Sum foi chamado duas vezes, porém a mensagem do resolver foi mostrada apenas uma vez. Isto é porque o método foi compilado apenas uma vez pelo JIT.

Lembra que falamos que todos os métodos compilados pelo JIT vão para o nosso resolver? Se nós tirarmos a condição do resolver que filtra apenas métodos com o nome "Sum":

```csharp
JitexManager.AddMethodResolver(context =>
{
    Console.WriteLine("Method to compile: " + context.Method.Name);
});
```

Você terá um resultado parecido com este:

```
Method to compiled: Int32 Sum(Int32, Int32)
Method to compiled: System.String ToString()
Method to compiled: System.String Int32ToDecStr(Int32)
Method to compiled: System.String UInt32ToDecStr(UInt32)
Method to compiled: Int32 CountDigits(UInt32)
Method to compiled: Void .cctor()

First random number: 2
Second random number: 4

Method to compiled: Void OnProcessExit()
Method to compiled: Void OnProcessExit()
Method to compiled: Enumerator GetEnumerator()
Method to compiled: Void .ctor(System.Collections.Generic.Dictionary`2[System.Int64,System.__Canon], Int32)
Method to compiled: Boolean MoveNext()
Method to compiled: Void Dispose()
Method to compiled: System.AppDomain get_CurrentDomain()
Method to compiled: Void .cctor()
Method to compiled: Void .ctor()
Method to compiled: Void .cctor()
Method to compiled: Void .ctor()
Method to compiled: Void DisposeOnShutdown(System.Object, System.EventArgs)
Method to compiled: Enumerator GetEnumerator()
Method to compiled: Void .ctor(System.Collections.Generic.List`1[System.__Canon])
Method to compiled: Boolean MoveNext()
Method to compiled: System.__Canon get_Current()
Method to compiled: Boolean TryGetTarget(System.__Canon ByRef)
Method to compiled: Void Dispose()
Method to compiled: Void Dispose(Boolean)
Method to compiled: Void Dispose()
Method to compiled: Void Dispose(Boolean)
Method to compiled: Void EventUnregister(Int64)
Method to compiled: UInt32 System.Diagnostics.Tracing.IEventProvider.EventUnregister(Int64)
Method to compiled: Void SuppressFinalize(System.Object)
Method to compiled: UInt32 System.Diagnostics.Tracing.IEventProvider.EventUnregister(Int64)
Method to compiled: Boolean MoveNextRare()
Method to compiled: Void Dispose()
```

Estes são os métodos compilados pelo JIT que o Jitex foi capaz de interceptar durante a execução.
