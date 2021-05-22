# O que é o Jitex?

Jitex é uma biblioteca para ajudar a controlar fluxos de aplicações feitas em .NET. Usando o Jitex, você é capaz de modificar e interceptar métodos em tempos de execução, sem a necessidade de grandes alterações no código ou no assembly gerado na compilação. Abaixo, você pode ver exemplos do que é possível fazer com Jitex:

- Substituir um método por outro
- Modificar o MSIL ou o código nativo de um método
- Desviar (Detour/Trampoline) a chamada de um método
- Injetar ou resolver metadatokens (possibilitando a utilização de tipos desconhecidos em tempo de compilação)
- Modificar strings
- Interceptar as chamadas de um método
- ...

> Todos os cenários citados acima funcionam com tipos genéricos

O principal objetivo do Jitex, é tornar as aplicações feitas em .NET mais fáceis de serem desenvolvidas e com maior performance. Abaixo é possível alguns usos do Jitex:

* Remoção de chamadas do AutoMapper
* InAsm (Assembly diretamente no C#)
* ReMock - Fácil mock de testes 