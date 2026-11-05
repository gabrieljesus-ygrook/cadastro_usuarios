---
description: "Tarefas de implementaÃ§Ã£o TDD para gerenciamento de usuÃ¡rios"
---

# Tarefas: Gerenciamento de usuÃ¡rios

**Entradas**: [spec.md](spec.md), [plan.md](plan.md), [research.md](research.md), [data-model.md](data-model.md), [contracts/openapi.yaml](contracts/openapi.yaml), [quickstart.md](quickstart.md)

**OrganizaÃ§Ã£o**: tarefas por histÃ³ria do usuÃ¡rio, com Red-Green-Refactor obrigatÃ³rio.

## Fase 1: ConfiguraÃ§Ã£o do projeto

**Objetivo**: criar a API, projetos de teste e infraestrutura inicial para TDD.

- [X] T001 Criar `CadastroUsuarios.sln` e o projeto ASP.NET Core Web API `src/CadastroUsuarios.Api/CadastroUsuarios.Api.csproj` com nullable habilitado.
- [X] T002 Criar o projeto de integraÃ§Ã£o xUnit `tests/CadastroUsuarios.IntegrationTests/CadastroUsuarios.IntegrationTests.csproj`.
- [X] T003 Adicionar referÃªncia Ã  API e pacotes de teste necessÃ¡rios em `tests/CadastroUsuarios.IntegrationTests/CadastroUsuarios.IntegrationTests.csproj`, incluindo `Microsoft.AspNetCore.Mvc.Testing`.
- [X] T004 Adicionar a referÃªncia do projeto de teste Ã  API em `CadastroUsuarios.sln` e tornar o entry point `Program` acessÃ­vel ao `WebApplicationFactory` em `src/CadastroUsuarios.Api/Program.cs`.
- [X] T005 Criar `tests/CadastroUsuarios.IntegrationTests/Infrastructure/ApiFactory.cs` com `WebApplicationFactory<Program>` e exigir `ConnectionStrings__TestDatabase` apontando para um banco SQL Server exclusivo de teste.
- [X] T006 Criar `.config/dotnet-tools.json` e registrar nele a ferramenta local `dotnet-ef` compatÃ­vel com EF Core 10.
- [X] T007 Adicionar pacotes `Microsoft.AspNetCore.Identity.EntityFrameworkCore`, `Microsoft.EntityFrameworkCore.SqlServer`, `Microsoft.EntityFrameworkCore.Design` e OpenAPI ao arquivo `src/CadastroUsuarios.Api/CadastroUsuarios.Api.csproj` usando versÃµes compatÃ­veis com .NET 10 / EF Core 10.
- [X] T008 Criar a estrutura `Features/Authentication`, `Features/Users`, `Infrastructure/Identity` e `Infrastructure/Persistence` sob `src/CadastroUsuarios.Api/` conforme `plan.md`.

## Fase 2: Fundamentos compartilhados

**Objetivo**: preparar persistÃªncia Identity, configuraÃ§Ã£o segura e composiÃ§Ã£o da API para as histÃ³rias.

- [X] T009 Criar `src/CadastroUsuarios.Api/Infrastructure/Identity/ApplicationUser.cs` herdando de `IdentityUser` e adicionar a propriedade obrigatÃ³ria `Name`.
- [X] T010 Criar `src/CadastroUsuarios.Api/Infrastructure/Persistence/ApplicationDbContext.cs` derivando de `IdentityDbContext<ApplicationUser>`.
- [X] T011 Completar `tests/CadastroUsuarios.IntegrationTests/Infrastructure/ApiFactory.cs` para aplicar migrations no banco SQL Server isolado de testes e limpar os dados entre casos.
- [X] T012 Escrever `tests/CadastroUsuarios.IntegrationTests/Infrastructure/IdentityConfigurationTests.cs` para verificar registro de SQL Server/Identity e polÃ­tica mÃ­nima de senha; executar `dotnet test` e confirmar as falhas esperadas antes da configuraÃ§Ã£o.
- [X] T013 Configurar em `src/CadastroUsuarios.Api/Program.cs` `ApplicationDbContext`, provider SQL Server, Identity com roles e lifetime scoped para fazer passar o teste de composiÃ§Ã£o.
- [X] T014 Configurar em `src/CadastroUsuarios.Api/Program.cs` polÃ­tica Identity com mÃ­nimo de 8 caracteres e sem exigÃªncias adicionais de composiÃ§Ã£o; executar o teste de configuraÃ§Ã£o e confirmar que passou.
- [X] T015 Documentar em `src/CadastroUsuarios.Api/appsettings.json` somente chaves nÃ£o secretas e manter connection strings locais em User Secrets.
- [X] T016 Criar migration inicial do schema Identity e confirmar os arquivos em `src/CadastroUsuarios.Api/Infrastructure/Persistence/Migrations/`.

## Fase 3: HistÃ³ria 1 â€” Autenticar como Master (Prioridade: P1)

**Objetivo**: provisionar o Master inicial e permitir que somente ele estabeleÃ§a sessÃ£o administrativa.

**VerificaÃ§Ã£o independente**: testes de integraÃ§Ã£o confirmam bootstrap idempotente, login Master vÃ¡lido, recusa de credenciais invÃ¡lidas e conta comum, sessÃ£o/logout, 401 sem autenticaÃ§Ã£o e 403 sem role Master.

### Red â€” testes primeiro

- [X] T017 [US1] Escrever em `tests/CadastroUsuarios.IntegrationTests/Features/Authentication/AuthEndpointsTests.cs` os testes de login Master vÃ¡lido/invÃ¡lido, recusa de conta comum, sessÃ£o, logout e antiforgery; executar `dotnet test` e confirmar falhas esperadas por endpoints ainda ausentes.
- [X] T018 [US1] Escrever em `tests/CadastroUsuarios.IntegrationTests/Features/Authentication/MasterProvisioningTests.cs` os testes de criaÃ§Ã£o e idempotÃªncia do bootstrap; executar `dotnet test` e confirmar falhas esperadas antes do provisionador.

### Green e Refactor â€” implementaÃ§Ã£o

- [X] T019 [US1] Criar `src/CadastroUsuarios.Api/Infrastructure/Identity/MasterUserProvisioner.cs` para garantir a role `Master` e criar o usuÃ¡rio inicial via `RoleManager`/`UserManager` usando configuraÃ§Ã£o externa.
- [X] T020 [US1] Implementar em `src/CadastroUsuarios.Api/Program.cs` o modo explÃ­cito `--provision-master`, sem rota HTTP e sem execuÃ§Ã£o automÃ¡tica no startup normal.
- [X] T021 [US1] Criar `src/CadastroUsuarios.Api/Features/Authentication/LoginRequest.cs` como contrato com `login` e `password`, sem expor entidades Identity.
- [X] T022 [US1] Implementar `POST /api/auth/login` em `src/CadastroUsuarios.Api/Features/Authentication/AuthEndpoints.cs`, emitindo cookie somente apÃ³s validar senha e role Master.
- [X] T023 [US1] Implementar `GET /api/auth/antiforgery`, `GET /api/auth/session` e `POST /api/auth/logout` em `src/CadastroUsuarios.Api/Features/Authentication/AuthEndpoints.cs`.
- [X] T024 [US1] Configurar cookies API, autorizaÃ§Ã£o Master, antiforgery e `ProblemDetails` em `src/CadastroUsuarios.Api/Program.cs`; mapear os endpoints, executar testes US1 e refatorar atÃ© ficarem verdes.

## Fase 4: HistÃ³ria 2 â€” Cadastrar usuÃ¡rio (Prioridade: P1)

**Objetivo**: permitir que Master autenticado crie usuÃ¡rio comum com nome, login exclusivo e senha protegida.

**VerificaÃ§Ã£o independente**: Master recebe 201 com `id`, `name` e `login`; login duplicado, campos ausentes e senha menor que 8 caracteres sÃ£o recusados; senha/hash nÃ£o aparecem na resposta.

### Red â€” testes primeiro

- [X] T025 [US2] Escrever em `tests/CadastroUsuarios.IntegrationTests/Features/Users/CreateUserTests.cs` testes de sucesso, login duplicado, formato e tamanho invÃ¡lidos do login, dados invÃ¡lidos, senha mÃ­nima, 401/403 e ausÃªncia de credenciais na resposta; executar `dotnet test` e confirmar falhas esperadas.

### Green e Refactor â€” implementaÃ§Ã£o

- [X] T026 [US2] Criar `src/CadastroUsuarios.Api/Features/Users/CreateUserRequest.cs` com campos obrigatÃ³rios `name`, `login` e `password`, refletindo o formato e limite de 30 caracteres do login e mÃ­nimo de 8 caracteres da senha.
- [X] T027 [US2] Criar `src/CadastroUsuarios.Api/Features/Users/UserResponse.cs` contendo apenas `id`, `name` e `login`.
- [X] T028 [US2] Implementar cadastro em `src/CadastroUsuarios.Api/Features/Users/UserEndpoints.cs` usando `UserManager<ApplicationUser>`, normalizaÃ§Ã£o de login, validaÃ§Ã£o Identity e criaÃ§Ã£o sem role Master.
- [X] T029 [US2] Converter login duplicado e falhas de validaÃ§Ã£o em respostas apropriadas em `src/CadastroUsuarios.Api/Features/Users/UserEndpoints.cs`.
- [X] T030 [US2] Mapear `POST /api/users` em `src/CadastroUsuarios.Api/Program.cs` com autorizaÃ§Ã£o Master, antiforgery e `201 Created` com `Location`; executar os testes US2 e refatorar atÃ© ficarem verdes.

## Fase 5: HistÃ³ria 3 â€” Consultar usuÃ¡rios (Prioridade: P2)

**Objetivo**: permitir ao Master listar usuÃ¡rios paginados e consultar uma conta individual.

**VerificaÃ§Ã£o independente**: resultados contÃªm somente `id`, `name` e `login`; paginaÃ§Ã£o Ã© respeitada; usuÃ¡rio ausente resulta em 404; autorizaÃ§Ã£o Master Ã© aplicada.

### Red â€” testes primeiro

- [X] T031 [US3] Escrever em `tests/CadastroUsuarios.IntegrationTests/Features/Users/ReadUsersTests.cs` testes de listagem, paginaÃ§Ã£o, detalhe, 404, 401/403 e ausÃªncia de campos internos; executar `dotnet test` e confirmar falhas esperadas.

### Green e Refactor â€” implementaÃ§Ã£o

- [X] T032 [US3] Criar `src/CadastroUsuarios.Api/Features/Users/UserPageResponse.cs` com `items`, `page`, `pageSize` e `totalCount`.
- [X] T033 [US3] Implementar `GET /api/users` em `src/CadastroUsuarios.Api/Features/Users/UserEndpoints.cs` com paginaÃ§Ã£o limitada, projeÃ§Ã£o pÃºblica e consulta somente leitura.
- [X] T034 [US3] Implementar `GET /api/users/{id}` em `src/CadastroUsuarios.Api/Features/Users/UserEndpoints.cs`, retornando 404 para conta ausente e sem campos internos de Identity.
- [X] T035 [US3] Mapear e proteger consultas com autorizaÃ§Ã£o Master em `src/CadastroUsuarios.Api/Program.cs`; executar testes US3 e refatorar atÃ© ficarem verdes.

## Fase 6: HistÃ³ria 4 â€” Editar perfil (Prioridade: P3)

**Objetivo**: permitir atualizar nome e/ou login sem alterar a senha.

**VerificaÃ§Ã£o independente**: consulta posterior reflete os novos dados; duplicidade e usuÃ¡rio ausente sÃ£o recusados; senha permanece inalterada.

### Red â€” testes primeiro

- [ ] T036 [US4] Escrever em `tests/CadastroUsuarios.IntegrationTests/Features/Users/UpdateUserTests.cs` testes de ediÃ§Ã£o parcial, login duplicado, formato e tamanho invÃ¡lidos do login, 404, 401/403 e senha inalterada; executar `dotnet test` e confirmar falhas esperadas.

### Green e Refactor â€” implementaÃ§Ã£o

- [ ] T037 [US4] Criar `src/CadastroUsuarios.Api/Features/Users/UpdateUserRequest.cs` com campos editÃ¡veis `name` e `login`, sem campo de senha.
- [ ] T038 [US4] Implementar `PUT /api/users/{id}` em `src/CadastroUsuarios.Api/Features/Users/UserEndpoints.cs` com atualizaÃ§Ã£o Identity, normalizaÃ§Ã£o de login, conflito e 404.
- [ ] T039 [US4] Mapear ediÃ§Ã£o com autorizaÃ§Ã£o Master e antiforgery em `src/CadastroUsuarios.Api/Program.cs`; executar testes US4 e refatorar atÃ© ficarem verdes.

## Fase 7: HistÃ³ria 5 â€” Alterar senha (Prioridade: P3)

**Objetivo**: permitir que Master altere a senha separadamente da ediÃ§Ã£o do perfil.

**VerificaÃ§Ã£o independente**: senha vÃ¡lida Ã© aceita; senha curta e conta ausente sÃ£o recusadas; resposta nÃ£o revela senha nem hash.

### Red â€” testes primeiro

- [ ] T040 [US5] Escrever em `tests/CadastroUsuarios.IntegrationTests/Features/Users/ChangeUserPasswordTests.cs` testes de troca vÃ¡lida, polÃ­tica mÃ­nima, 404, 401/403 e resposta sem credenciais; executar `dotnet test` e confirmar falhas esperadas.

### Green e Refactor â€” implementaÃ§Ã£o

- [ ] T041 [US5] Criar `src/CadastroUsuarios.Api/Features/Users/ChangeUserPasswordRequest.cs` com campo obrigatÃ³rio `newPassword` e mÃ­nimo de 8 caracteres.
- [ ] T042 [US5] Implementar `PUT /api/users/{id}/password` em `src/CadastroUsuarios.Api/Features/Users/UserEndpoints.cs` usando `GeneratePasswordResetTokenAsync` e `ResetPasswordAsync`, retornando 204 sem credencial.
- [ ] T043 [US5] Mapear a troca de senha com autorizaÃ§Ã£o Master e antiforgery em `src/CadastroUsuarios.Api/Program.cs`; executar testes US5 e refatorar atÃ© ficarem verdes.

## Fase 8: HistÃ³ria 6 â€” Excluir usuÃ¡rio (Prioridade: P4)

**Objetivo**: excluir usuÃ¡rios comuns e preservar ao menos um Master ativo, inclusive sob concorrÃªncia.

**VerificaÃ§Ã£o independente**: usuÃ¡rio comum excluÃ­do desaparece das consultas; conta ausente resulta em 404; nÃ£o Ã© possÃ­vel excluir o Ãºltimo Master, mesmo com exclusÃµes concorrentes.

### Red â€” testes primeiro

- [ ] T044 [US6] Escrever em `tests/CadastroUsuarios.IntegrationTests/Features/Users/DeleteUserTests.cs` testes de exclusÃ£o comum, 404, 401/403, Ãºltimo Master e duas exclusÃµes concorrentes de Masters; executar `dotnet test` e confirmar falhas esperadas.

### Green e Refactor â€” implementaÃ§Ã£o

- [ ] T045 [US6] Implementar `DELETE /api/users/{id}` em `src/CadastroUsuarios.Api/Features/Users/UserEndpoints.cs` com verificaÃ§Ã£o de existÃªncia e exclusÃ£o via `UserManager`.
- [ ] T046 [US6] Executar a contagem de membros Master e a exclusÃ£o dentro de uma transaÃ§Ã£o `SERIALIZABLE` no mesmo `ApplicationDbContext`, retornando conflito se a conta for a Ãºltima, em `src/CadastroUsuarios.Api/Features/Users/UserEndpoints.cs`.
- [ ] T047 [US6] Mapear exclusÃ£o com autorizaÃ§Ã£o Master e antiforgery em `src/CadastroUsuarios.Api/Program.cs`; executar testes US6 e refatorar atÃ© ficarem verdes.

## Fase 9: Polimento e alinhamento de contrato

**Objetivo**: manter documentaÃ§Ã£o, comportamento e suÃ­te de testes coerentes.

- [ ] T048 Atualizar `specs/001-user-management/quickstart.md` com comandos efetivos de configuraÃ§Ã£o, migration, bootstrap, testes e verificaÃ§Ã£o manual.
- [ ] T049 Revisar `specs/001-user-management/contracts/openapi.yaml` contra os endpoints implementados e ajustar somente divergÃªncias aprovadas.
- [ ] T050 Executar `dotnet test CadastroUsuarios.sln` e revisar o resultado antes de encerrar a implementaÃ§Ã£o.
- [ ] T051 Revisar `src/CadastroUsuarios.Api/Program.cs` e arquivos das features para confirmar ausÃªncia de registro pÃºblico, segredos versionados e exposiÃ§Ã£o de dados de senha.

## DependÃªncias e ordem

- Fase 1 â†’ Fase 2 â†’ US1 â†’ US2 â†’ US3 â†’ US4 â†’ US5 â†’ US6 â†’ Polimento.
- Cada histÃ³ria comeÃ§a pela fase Red, continua em Green e termina em Refactor; nÃ£o comeÃ§ar cÃ³digo de produÃ§Ã£o da histÃ³ria antes de observar a falha do teste correspondente.
- US1 e US2 sÃ£o o MVP inicial: autenticar Master e cadastrar usuÃ¡rios.
- As histÃ³rias ficam sequenciais para esta implementaÃ§Ã£o individual; compartilham `UserEndpoints.cs`, `Program.cs` e a fixture de integraÃ§Ã£o.

## EstratÃ©gia TDD

Para cada histÃ³ria: escrever um teste que expresse um cenÃ¡rio de aceitaÃ§Ã£o; executar `dotnet test` e observar a falha esperada; implementar o mÃ­nimo necessÃ¡rio; executar a suÃ­te novamente; refatorar mantendo tudo verde. Os testes de integraÃ§Ã£o usam uma connection string exclusiva de SQL Server de teste, nunca dados de produÃ§Ã£o. Usar testes unitÃ¡rios para lÃ³gica isolada quando isso trouxer valor sem substituir testes de integraÃ§Ã£o das regras HTTP e relacionais.
