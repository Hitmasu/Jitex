# Instalação

O Jitex pode ser obtido através do pacote nuget: [![Nuget](https://img.shields.io/nuget/vpre/Jitex)](https://www.nuget.org/packages/Jitex/) ou através do CLI:

dotnet cli:

`dotnet add package Jitex`

Package Manager:

`Install-Package Jitex`

> Aplicações feita em .NET Framework também devem instalar o pacote .NETStandard.Library e o Mono.Posix.NETFramework.

# Iniciando

Vamos fazer uma simples introdução demonstrando a biblioteca.

Crie um Console Application que faça a soma de 2 números inteiros:

```csharp
class Program
{
    static void Main(string[] args)
    {
        int sum = Sum(5, 5);
        Console.WriteLine(sum);
    }

    static int Sum(int n1, int n2) => n1 + n2;
}
```

O código acima apenas retorna a soma de dois números inteiros e mostra em tela.

O Jitex, possui uma classe estática chamada `JitexManager`, ela é a responsável de adicionar os hooks desejados em nossa aplicação.

Usando a classe JitexManager, vamos criar e passar um MethodResolver para ela:

```csharp
class Program
{
    static void Main(string[] args)
    {
        JitexManager.AddMethodResolver(MethodResolver);
        int sum = Sum(5, 5);
        Console.WriteLine(sum);
    }

    static int Sum(int n1, int n2) => n1 + n2;
    
    private static void MethodResolver(MethodContext context)
    {
        throw new NotImplementedException();
    }
}
```

> Observe como a adição do MethodResolver ficou antes da chamada do método Sum

O MethodResolver, é o nosso hook que vai capturar todo método que está para ser compilado pelo JIT. Quando o método Sum for chamado pela primeira vez, o JIT precisará compilar o MSIL para código nativo. Quando esta compilação ocorrer, o nosso hook será chamado nos dando a possibilidade de alterar o método antes de ser compilado definitivamente para código nativo. O parâmetro **context** no nosso resolver é quem contém informações sobre o método que vai ser compilado.

Por se tratar apenas de uma introdução, vamos apenas substituir o método por um outro método:

```csharp
class Program
{
    static void Main(string[] args)
    {
        JitexManager.AddMethodResolver(MethodResolver);
        int sum = Sum(5, 5);
        Console.WriteLine(sum);
    }

    static int Sum(int n1, int n2) => n1 + n2;

    public static int Mul(int n1, int n2) => n1 * n2;
    
    private static void MethodResolver(MethodContext context)
    {
        if (context.Method.Name == "Sum")
        {
            MethodInfo methodToReplace = typeof(Program).GetMethod("Mul");
            context.ResolveMethod(methodToReplace);
        }
    }
}
```

No nosso resolver (MethodResolver), validamos se o método que será compilado é o nosso método Sum. Como dito anteriormente, todos os métodos à serem compilados pelo JIT, vão passar pelo nosso resolver, por isso a necessidade desta validação. Sendo o método que desejamos modificar, informamos para o Jitex, através do context, que o método deve ser substituído pelo método Mul.

> No resolver, a forma como validamos se é o nosso método, não é uma forma segura, pois pode ocorrer de outro método Sum existir ao longo da aplicação e acabarmos substituir outro método acidentalmente acidentalmente.

O resultado final deverá ser:

```bash
25
```

