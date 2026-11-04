# Pesquisa técnica: Gerenciamento de usuários

Data: 2026-11-04

## Autenticação e autorização

**Decisão**: usar ASP.NET Core Identity com usuário customizado, role `Master` e cookie de autenticação para o cliente browser futuro. A API terá login próprio por `login` e `senha`, logout e consulta da sessão; não mapeará registro público. As rotas CRUD exigem a role Master. Cookies são adequados para browser quando a arquitetura permite same-origin/proxy; como cookies são enviados automaticamente, operações mutáveis precisam de proteção antiforgery. Se a futura hospedagem Angular/API for cross-origin, CORS com credenciais e configuração de cookie/antiforgery precisarão ser fechados antes do frontend.

A documentação do Identity API descreve autenticação para SPAs e observa que seus tokens próprios não são JWT nem substituem um provedor de identidade completo. Um endpoint de login próprio é adequado porque o domínio usa login em vez do e-mail usado pelo endpoint padrão. Documentação: [Identity API para SPA (.NET 10)](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity-api-authorization?view=aspnetcore-10.0), [autenticação em endpoints de API (.NET 10)](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/api-endpoint-auth?view=aspnetcore-10.0), [CORS (.NET 10)](https://learn.microsoft.com/en-us/aspnet/core/security/cors?view=aspnetcore-10.0), [antiforgery (.NET 10)](https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery?view=aspnetcore-10.0).

## Senhas e Master inicial

**Decisão**: usar `UserManager`/`SignInManager` e o hasher do Identity; nunca armazenar senha própria nem usar `HasData` para credenciais. Senhas devem ter pelo menos 8 caracteres, sem regra extra de composição nesta versão. O valor persistido é um hash unidirecional (não criptografia reversível); o Identity verifica a senha por meio do hasher. O Master inicial é criado por comando administrativo explícito que usa `RoleManager` e `UserManager`, sem rota pública e sem execução automática em cada instância da API.

User Secrets ficam fora do diretório do projeto e são apropriados para desenvolvimento, mas não são criptografados; produção deve usar secret store. O EF Core alerta que contas Identity e hashing exigem lógica de aplicação, e que seed automático pode conflitar quando há múltiplas instâncias. Fontes: [Identity model customizado (.NET 10)](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/customize-identity-model?view=aspnetcore-10.0), [configuração do Identity (.NET 10)](https://learn.microsoft.com/en-us/aspnet/core/security/identity-configuration?view=aspnetcore-10.0), [`UserManager<TUser>` (.NET 10)](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.identity.usermanager-1?view=aspnetcore-10.0), [seeding no EF Core](https://learn.microsoft.com/en-us/ef/core/modeling/data-seeding), [segredos de aplicação (.NET 10)](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-10.0).

## EF Core e SQL Server

**Decisão**: provider `Microsoft.EntityFrameworkCore.SqlServer` na linha 10.x, `AddDbContext` scoped com `UseSqlServer`, migrations no projeto da API e `dotnet-ef` em tool manifest. Identity já fornece o modelo de usuário, roles e o índice único de nome normalizado; usar o índice do Identity para concorrência/unicidade em vez de depender só da validação prévia em aplicação. SQL Server Provider e versões: [provider SQL Server](https://learn.microsoft.com/en-us/ef/core/providers/sql-server/), [providers suportados](https://learn.microsoft.com/en-us/ef/core/providers/); [configuração de DbContext](https://learn.microsoft.com/en-us/ef/core/dbcontext-configuration/); [índices EF Core](https://learn.microsoft.com/en-us/ef/core/modeling/indexes).

Manter migrations no repositório e aplicar explicitamente em desenvolvimento. Para produção, gerar bundle ou script SQL revisável e aplicar com identidade de deploy de schema. Fontes: [migrations](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/), [aplicação de migrations](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/applying), [ferramentas EF Core CLI](https://learn.microsoft.com/en-us/ef/core/cli/dotnet).

## Separação entre leitura e escrita (CQRS)

**Decisão**: não usar CQRS completo nem adicionar MediatR/um framework de dispatch nesta primeira versão. A API é CRUD simples e usa um único modelo e banco; esse padrão acrescentaria estrutura sem uma exigência atual de escala ou regras complexas. Manter operações de leitura sem efeitos colaterais e operações de escrita separadas, mas no mesmo projeto e no mesmo banco. Para eficiência, as consultas selecionam apenas os campos necessários, não rastreiam entidades quando são somente leitura e limitam/paginam resultados. Revisitar a decisão quando requisitos ou medições mostrarem benefício concreto.

A documentação da Microsoft aponta CQRS como útil quando há diferenças relevantes entre demandas de leitura e escrita, e reconhece que CRUD simples pode ser atendido com uma solução mais direta. EF Core recomenda projeção, limitar resultados e considerar consultas sem tracking conforme o caso. Fontes: [padrão CQRS no Azure Architecture Center](https://learn.microsoft.com/en-us/azure/architecture/patterns/cqrs), [CQRS simplificado em .NET](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/apply-simplified-microservice-cqrs-ddd-patterns), [consultas eficientes no EF Core](https://learn.microsoft.com/en-us/ef/core/performance/efficient-querying).

## Estratégia de testes e TDD

**Decisão**: seguir Red-Green-Refactor em cada comportamento. Usar xUnit e `WebApplicationFactory` no projeto de integração para exercitar endpoints HTTP, cookies, autorização e antiforgery. Para regras que dependem de SQL Server, executar integração contra um banco de teste isolado configurado fora do repositório; não usar o provider EF Core InMemory como substituto relacional. Preferir um conjunto pequeno de cenários representativos de cada operação CRUD, em vez de duplicar toda regra em múltiplas camadas de teste. Criar um projeto unitário separado somente se surgir lógica isolada que justifique essa divisão.

A documentação ASP.NET Core recomenda `WebApplicationFactory`/`TestServer` para testes de integração e diferencia esses testes dos unitários; a documentação EF/ASP.NET alerta que o provider InMemory é limitado e recomenda uma alternativa relacional para testes. Fontes: [testes de integração ASP.NET Core (.NET 10)](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0), [testes unitários em C# com xUnit](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-with-dotnet-test), [`dotnet test`](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-test).

## Transação para preservar o último Master

**Decisão**: verificar a quantidade de membros da role `Master` e excluir uma conta Master dentro da mesma transação SQL Server `SERIALIZABLE`. Esse nível mantém os locks de leitura e protege contra mudanças concorrentes no conjunto consultado até a transação terminar. Testar duas exclusões concorrentes com a integração SQL Server. `SERIALIZABLE` é usado somente nesta operação crítica para limitar o custo de bloqueio. Fonte: [guia de locks e níveis de isolamento do SQL Server](https://learn.microsoft.com/en-us/sql/relational-databases/sql-server-transaction-locking-and-row-versioning-guide?view=sql-server-ver17).

## Decisões ainda necessárias antes da implementação

- Confirmar a origem de hospedagem do Angular e da API quando o frontend começar; isso decide detalhes de CORS e cookies. A API começa preparada para cookies e antiforgery.
- Escolher nomes finais de solution/projetos e strings de conexão no início da implementação, mantendo a estrutura definida no plano.

As escolhas de autenticação, separação da troca de senha e proteção do Master foram confirmadas pelo usuário (`1A`, `2A`, `3A`) em 2026-11-04.
