# Plano de implementação: Gerenciamento de usuários

**Branch**: `001-user-management` | **Data**: 2026-11-04 | **Spec**: [spec.md](spec.md)

## Resumo

Construir uma API ASP.NET Core em .NET 10 para autenticação do Master e gerenciamento de usuários. A solução usará ASP.NET Core Identity para contas, papéis, hashing de senhas e autenticação por cookie; Entity Framework Core 10 com SQL Server para persistência; e contratos HTTP próprios para não expor entidades de persistência. Só o Master autenticado poderá acessar cadastro, consulta, edição, troca de senha e exclusão. O primeiro Master será provisionado por comando administrativo explícito, com credenciais vindas de configuração externa, sem cadastro público.

Decisões de produto fixadas nesta especificação: (1) operações administrativas só para Master autenticado; (2) troca de senha é operação separada da edição do perfil; (3) a última conta Master não pode ser excluída nem perder a role Master.

## Contexto técnico

**Linguagem/versão**: C# 14 / .NET 10, com nullable reference types habilitado.
**Dependências principais**: ASP.NET Core Web API, ASP.NET Core Identity, EF Core 10, `Microsoft.EntityFrameworkCore.SqlServer`, OpenAPI e ferramentas EF Core.
**Armazenamento**: SQL Server; `ApplicationDbContext` deriva de `IdentityDbContext<ApplicationUser>`.
**Testes**: TDD obrigatório no desenvolvimento. Um projeto xUnit de integração ASP.NET Core com `WebApplicationFactory` cobre HTTP, Identity e persistência SQL Server por comportamento. Adicionar projeto unitário separado somente se surgir lógica isolada que justifique essa divisão. Cada história começa por teste que falha, seguido de implementação e refatoração.
**Plataforma alvo**: API HTTP executada em .NET 10; Angular será uma etapa futura.
**Tipo de projeto**: serviço web backend.
**Metas de desempenho**: não há metas de throughput ou latência definidas para este exercício; usar consultas assíncronas e paginação da lista para evitar retorno sem limite.
**Restrições**: sem cadastro público; segredos fora do Git; nenhuma senha em texto puro ou resposta; acesso administrativo restrito a Master; migrations revisadas e versionadas.
**Escala/escopo**: aplicação de estudo e administração de usuários, sem volume definido. A primeira versão contempla login/logout, CRUD administrativo e alteração administrativa de senha.

## Verificação da constituição

- SOLID com propósito: **passa**. Limites entre HTTP, aplicação e persistência serão claros; não se adicionará repositório genérico sem necessidade concreta.
- C# moderno, nullability e semântica explícita: **passa**.
- Contratos HTTP separados do EF: **passa**. DTOs próprios serão usados nas entradas e respostas.
- I/O assíncrono e cancelável: **passa**. Chamadas de banco e Identity usarão APIs assíncronas e `CancellationToken` quando oferecido.
- TDD: **passa**. Os comportamentos serão especificados em testes antes do código de produção, seguindo Red-Green-Refactor.
- Segurança e simplicidade: **passa com decisões registradas**. Identity gerencia hashes; cookies HttpOnly protegem sessão do browser; antiforgery protege ações mutáveis; bootstrap é explícito e fora do tráfego HTTP.

## Decisões de arquitetura

1. **Identity para contas e senha**: `ApplicationUser : IdentityUser` acrescenta `Name`; `UserName` representa o login. As operações usam `UserManager` e `SignInManager`; não haverá entidade nem hash de senha próprios. Configurar senha com mínimo de 8 caracteres e sem exigências adicionais de composição nesta versão. Identity armazena um hash unidirecional; não se cifra a senha para depois descriptografá-la.
2. **Role Master**: habilitar roles e autorizar rotas por role/policy. Contas criadas pelo CRUD recebem apenas o papel de usuário comum (sem papel administrativo).
3. **Autenticação para browser**: cookie de autenticação HttpOnly, Secure em produção e SameSite configurado conforme hospedagem. Login será um endpoint próprio por usar `login` em vez de e-mail. Não expor endpoints públicos de registro nem o endpoint genérico de registro do Identity. Usar endpoints API do ASP.NET Core para retornar 401/403 sem redirecionamento HTML.
4. **CSRF e origem do Angular**: exigir token antiforgery para operações autenticadas que alteram estado. O desenho assume API e Angular servidos pela mesma origem ou por proxy reverso. Se a hospedagem for cross-origin, CORS com credenciais, domínio/SameSite de cookie e emissão/entrega de antiforgery deverão ser configurados e revisados antes do frontend.
5. **Login exclusivo e normalizado**: aceitar de 1 a 30 caracteres ASCII entre letras, números, ponto e sublinhado; ponto não pode iniciar/terminar nem aparecer em sequência. Usar o normalizador de nomes do Identity e seu índice único em `NormalizedUserName`. Login será tratado sem distinção entre maiúsculas/minúsculas após normalização; espaços em volta serão removidos no limite HTTP. O banco continua sendo a garantia final de unicidade; conflitos serão convertidos em resposta de validação sem revelar detalhes internos.
6. **Bootstrap do primeiro Master**: comando explícito executado pela API em modo administrativo, não um hosted service em cada inicialização nem uma rota HTTP. Lê login/senha de configuração externa, cria role e usuário através de `RoleManager`/`UserManager`, e é idempotente. Em desenvolvimento, User Secrets; em produção, secret store do ambiente. Remover/desabilitar os valores de bootstrap após provisionar.
7. **Persistência e migrations**: `ApplicationDbContext` e migrations no projeto da API nesta primeira versão. `dotnet-ef` como ferramenta local versionada e pacote `Microsoft.EntityFrameworkCore.Design`. Aplicação de migrations em desenvolvimento é explícita; em produção usar bundle/script SQL revisável e identidade de deploy separada da identidade runtime.
8. **Proteção do último Master**: ao excluir uma conta Master, permitir a operação somente se restar ao menos outra conta Master ativa; contar membros Master e excluir dentro de uma transação SQL Server `SERIALIZABLE` no mesmo `ApplicationDbContext`. Não há endpoint de promoção/rebaixamento nesta versão. O bootstrap inicial cria um Master; a regra também protege o sistema se outra conta Master for provisionada por processo administrativo.
9. **Consultas sem CQRS completo**: para este CRUD inicial, não adotar um framework/mediator CQRS, banco de leitura separado ou event sourcing. Manter consultas e comandos simples dentro da feature `Users`, com responsabilidades e métodos distintos. Consultas de leitura projetam apenas `id`, `name` e `login`, usam `AsNoTracking` quando materializarem entidades, paginação e índices apropriados. Revisitar CQRS se surgirem regras de escrita complexas, requisitos de leitura diferentes, escala comprovada ou necessidade de modelos de leitura separados.
10. **TDD**: cada mudança de comportamento começa por um teste que falha pelo motivo esperado; implementar a menor mudança que o faça passar; refatorar mantendo a suíte verde. Usar xUnit, `WebApplicationFactory` para testes de integração HTTP, e SQL Server de teste separado para confirmar persistência e invariantes relacionais. Não usar EF Core InMemory para afirmar comportamento de SQL Server.
11. **Redefinição administrativa de senha**: usar `UserManager.GeneratePasswordResetTokenAsync` e `ResetPasswordAsync`; o Master não precisa conhecer a senha anterior e a senha nunca passa por APIs de armazenamento próprias.

## Estrutura do projeto

```text
src/
└── CadastroUsuarios.Api/
    ├── Features/
    │   ├── Authentication/       # login, logout, identidade da sessão
    │   └── Users/                # endpoints, casos de uso e contratos de usuário
    ├── Infrastructure/
    │   ├── Identity/             # ApplicationUser, roles e bootstrap explícito
    │   └── Persistence/          # DbContext, configurações e migrations
    ├── Program.cs                # composição de DI, autenticação e middleware
    └── appsettings.json          # apenas configuração não secreta
tests/
└── CadastroUsuarios.IntegrationTests/
```

**Decisão de estrutura**: uma API e dois projetos de testes organizados por funcionalidade. Não separar em múltiplos projetos de produção nesta fase; adicionar projetos só quando uma fronteira real justificar o custo.

## Artefatos do plano

- [Pesquisa técnica](research.md)
- [Modelo de dados](data-model.md)
- [Contrato OpenAPI](contracts/openapi.yaml)
- [Guia de início e verificação manual](quickstart.md)

`tasks.md` será criado posteriormente pelo `$speckit-tasks`, depois de revisar este plano.

## Acompanhamento de complexidade

Sem violações da constituição. O uso do Identity acrescenta tabelas e serviços, mas evita implementação própria de armazenamento de senha, autenticação e roles. Não será criado repositório genérico sem requisito que o justifique.
