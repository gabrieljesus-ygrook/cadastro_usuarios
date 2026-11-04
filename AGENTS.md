# Instruções do projeto — Cadastro de Usuários

## Contexto e escopo

- O escopo atual é o backend de uma API de cadastro de usuários em C# com .NET 10, Entity Framework Core e SQL Server.
- O frontend Angular fica para uma etapa futura. Não crie frontend nem outros serviços sem alinhamento explícito.
- Requisitos de domínio, campos, regras de validação, endpoints e decisões arquiteturais devem ser definidos antes de implementá-los; não os invente por conveniência.
- Converse e escreva a documentação do projeto em português brasileiro, salvo pedido em outro idioma.

## Princípios de arquitetura e código

- Aplique SOLID para manter responsabilidades coesas, dependências explícitas e componentes substituíveis quando isso trouxer valor real.
- Prefira composição. Crie interfaces e abstrações nas fronteiras necessárias ou quando houver uma razão concreta; evite camadas e padrões sem necessidade demonstrada.
- Siga a skill `modern-csharp-coding-standards` como referência para C# moderno, sem tratar cada recomendação como regra mecânica quando ela prejudicar clareza ou compatibilidade com EF Core.
- Habilite e respeite nullable reference types. Trate valores nulos explicitamente.
- Prefira imutabilidade. Use `record` para DTOs e mensagens quando a semântica de valor for adequada; modele entidades do EF Core de acordo com suas necessidades de identidade e persistência.
- Use `readonly record struct` para value objects quando invariantes e semântica de valor justificarem esse formato. Não adicione conversões implícitas.
- Use pattern matching e recursos modernos de C# quando deixarem a intenção mais clara.
- Operações de I/O devem ser assíncronas e aceitar `CancellationToken`. Não bloqueie operações assíncronas com `.Result` ou `.Wait()`.
- Mantenha contratos HTTP separados dos detalhes de persistência; não exponha entidades do EF Core diretamente como contratos da API.
- Trate dados pessoais e credenciais com cuidado: não grave segredos no repositório, não registre credenciais em logs e nunca armazene senhas em texto puro.
- Priorize a solução mais simples que cumpra os requisitos. Otimizações e abstrações adicionais precisam de uma necessidade concreta.

## Fluxo de trabalho com Spec Kit

- Use o fluxo Spec Kit para orientar novas funcionalidades: `$speckit-specify`, esclarecimento de ambiguidades quando necessário, `$speckit-plan`, `$speckit-tasks` e `$speckit-implement`.
- Use `$speckit-analyze` quando for útil verificar a consistência entre especificação, plano e tarefas.
- Não use a skill `create-plan`; o planejamento de funcionalidades será feito pelo `$speckit-plan`.
- Antes de implementar, alinhe com o usuário a especificação e as decisões que ainda estiverem em aberto.
- Mantenha este arquivo como orientação duradoura do repositório. Os arquivos locais do Spec Kit são ignorados pelo Git; suas decisões devem permanecer alinhadas com estas instruções.

## Desenvolvimento e aprendizado

- Trabalhe em modo de programação em dupla, ajudando o usuário a desenvolver o projeto e a entender as decisões técnicas.
- Divida o trabalho em tarefas pequenas. Antes de cada etapa, explique brevemente os conceitos e as opções relevantes.
- Quando fizer sentido, convide o usuário a tentar uma parte do código e depois revise a solução, explicando os ajustes e o raciocínio.
- Se o usuário travar ou pedir implementação direta, avance com ele passo a passo ou implemente a parte solicitada e explique como funciona.
- Ao concluir uma etapa significativa, resuma o que foi aprendido, as decisões tomadas e o próximo passo previsto.
