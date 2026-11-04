---
description: "Tarefas de implementação TDD para gerenciamento de usuários"
---

# Tarefas: Gerenciamento de usuários

**Entradas**: [spec.md](spec.md), [plan.md](plan.md), [research.md](research.md), [data-model.md](data-model.md), [contracts/openapi.yaml](contracts/openapi.yaml), [quickstart.md](quickstart.md)

**Organização**: tarefas por história do usuário, com Red-Green-Refactor obrigatório.

## Fase 1: Configuração do projeto

**Objetivo**: criar a API, projetos de teste e infraestrutura inicial para TDD.

- [X] T001 Criar `CadastroUsuarios.sln` e o projeto ASP.NET Core Web API `src/CadastroUsuarios.Api/CadastroUsuarios.Api.csproj` com nullable habilitado.
- [X] T002 Criar o projeto de integração xUnit `tests/CadastroUsuarios.IntegrationTests/CadastroUsuarios.IntegrationTests.csproj`.
- [X] T003 Adicionar referência à API e pacotes de teste necessários em `tests/CadastroUsuarios.IntegrationTests/CadastroUsuarios.IntegrationTests.csproj`, incluindo `Microsoft.AspNetCore.Mvc.Testing`.
- [X] T004 Adicionar a referência do projeto de teste à API em `CadastroUsuarios.sln` e tornar o entry point `Program` acessível ao `WebApplicationFactory` em `src/CadastroUsuarios.Api/Program.cs`.
- [X] T005 Criar `tests/CadastroUsuarios.IntegrationTests/Infrastructure/ApiFactory.cs` com `WebApplicationFactory<Program>` e exigir `ConnectionStrings__TestDatabase` apontando para um banco SQL Server exclusivo de teste.
- [X] T006 Criar `.config/dotnet-tools.json` e registrar nele a ferramenta local `dotnet-ef` compatível com EF Core 10.
- [X] T007 Adicionar pacotes `Microsoft.AspNetCore.Identity.EntityFrameworkCore`, `Microsoft.EntityFrameworkCore.SqlServer`, `Microsoft.EntityFrameworkCore.Design` e OpenAPI ao arquivo `src/CadastroUsuarios.Api/CadastroUsuarios.Api.csproj` usando versões compatíveis com .NET 10 / EF Core 10.
- [X] T008 Criar a estrutura `Features/Authentication`, `Features/Users`, `Infrastructure/Identity` e `Infrastructure/Persistence` sob `src/CadastroUsuarios.Api/` conforme `plan.md`.

## Fase 2: Fundamentos compartilhados

**Objetivo**: preparar persistência Identity, configuração segura e composição da API para as histórias.

- [X] T009 Criar `src/CadastroUsuarios.Api/Infrastructure/Identity/ApplicationUser.cs` herdando de `IdentityUser` e adicionar a propriedade obrigatória `Name`.
- [X] T010 Criar `src/CadastroUsuarios.Api/Infrastructure/Persistence/ApplicationDbContext.cs` derivando de `IdentityDbContext<ApplicationUser>`.
- [X] T011 Completar `tests/CadastroUsuarios.IntegrationTests/Infrastructure/ApiFactory.cs` para aplicar migrations no banco SQL Server isolado de testes e limpar os dados entre casos.
- [X] T012 Escrever `tests/CadastroUsuarios.IntegrationTests/Infrastructure/IdentityConfigurationTests.cs` para verificar registro de SQL Server/Identity e política mínima de senha; executar `dotnet test` e confirmar as falhas esperadas antes da configuração.
- [X] T013 Configurar em `src/CadastroUsuarios.Api/Program.cs` `ApplicationDbContext`, provider SQL Server, Identity com roles e lifetime scoped para fazer passar o teste de composição.
- [ ] T014 Configurar em `src/CadastroUsuarios.Api/Program.cs` política Identity com mínimo de 8 caracteres e sem exigências adicionais de composição; executar o teste de configuração e confirmar que passou.
- [ ] T015 Documentar em `src/CadastroUsuarios.Api/appsettings.json` somente chaves não secretas e manter connection strings locais em User Secrets.
- [ ] T016 Criar migration inicial do schema Identity e confirmar os arquivos em `src/CadastroUsuarios.Api/Infrastructure/Persistence/Migrations/`.

## Fase 3: História 1 — Autenticar como Master (Prioridade: P1)

**Objetivo**: provisionar o Master inicial e permitir que somente ele estabeleça sessão administrativa.

**Verificação independente**: testes de integração confirmam bootstrap idempotente, login Master válido, recusa de credenciais inválidas e conta comum, sessão/logout, 401 sem autenticação e 403 sem role Master.

### Red — testes primeiro

- [ ] T017 [US1] Escrever em `tests/CadastroUsuarios.IntegrationTests/Features/Authentication/AuthEndpointsTests.cs` os testes de login Master válido/inválido, recusa de conta comum, sessão, logout e antiforgery; executar `dotnet test` e confirmar falhas esperadas por endpoints ainda ausentes.
- [ ] T018 [US1] Escrever em `tests/CadastroUsuarios.IntegrationTests/Features/Authentication/MasterProvisioningTests.cs` os testes de criação e idempotência do bootstrap; executar `dotnet test` e confirmar falhas esperadas antes do provisionador.

### Green e Refactor — implementação

- [ ] T019 [US1] Criar `src/CadastroUsuarios.Api/Infrastructure/Identity/MasterUserProvisioner.cs` para garantir a role `Master` e criar o usuário inicial via `RoleManager`/`UserManager` usando configuração externa.
- [ ] T020 [US1] Implementar em `src/CadastroUsuarios.Api/Program.cs` o modo explícito `--provision-master`, sem rota HTTP e sem execução automática no startup normal.
- [ ] T021 [US1] Criar `src/CadastroUsuarios.Api/Features/Authentication/LoginRequest.cs` como contrato com `login` e `password`, sem expor entidades Identity.
- [ ] T022 [US1] Implementar `POST /api/auth/login` em `src/CadastroUsuarios.Api/Features/Authentication/AuthEndpoints.cs`, emitindo cookie somente após validar senha e role Master.
- [ ] T023 [US1] Implementar `GET /api/auth/antiforgery`, `GET /api/auth/session` e `POST /api/auth/logout` em `src/CadastroUsuarios.Api/Features/Authentication/AuthEndpoints.cs`.
- [ ] T024 [US1] Configurar cookies API, autorização Master, antiforgery e `ProblemDetails` em `src/CadastroUsuarios.Api/Program.cs`; mapear os endpoints, executar testes US1 e refatorar até ficarem verdes.

## Fase 4: História 2 — Cadastrar usuário (Prioridade: P1)

**Objetivo**: permitir que Master autenticado crie usuário comum com nome, login exclusivo e senha protegida.

**Verificação independente**: Master recebe 201 com `id`, `name` e `login`; login duplicado, campos ausentes e senha menor que 8 caracteres são recusados; senha/hash não aparecem na resposta.

### Red — testes primeiro

- [ ] T025 [US2] Escrever em `tests/CadastroUsuarios.IntegrationTests/Features/Users/CreateUserTests.cs` testes de sucesso, login duplicado, formato e tamanho inválidos do login, dados inválidos, senha mínima, 401/403 e ausência de credenciais na resposta; executar `dotnet test` e confirmar falhas esperadas.

### Green e Refactor — implementação

- [ ] T026 [US2] Criar `src/CadastroUsuarios.Api/Features/Users/CreateUserRequest.cs` com campos obrigatórios `name`, `login` e `password`, refletindo o formato e limite de 30 caracteres do login e mínimo de 8 caracteres da senha.
- [ ] T027 [US2] Criar `src/CadastroUsuarios.Api/Features/Users/UserResponse.cs` contendo apenas `id`, `name` e `login`.
- [ ] T028 [US2] Implementar cadastro em `src/CadastroUsuarios.Api/Features/Users/UserEndpoints.cs` usando `UserManager<ApplicationUser>`, normalização de login, validação Identity e criação sem role Master.
- [ ] T029 [US2] Converter login duplicado e falhas de validação em respostas apropriadas em `src/CadastroUsuarios.Api/Features/Users/UserEndpoints.cs`.
- [ ] T030 [US2] Mapear `POST /api/users` em `src/CadastroUsuarios.Api/Program.cs` com autorização Master, antiforgery e `201 Created` com `Location`; executar os testes US2 e refatorar até ficarem verdes.

## Fase 5: História 3 — Consultar usuários (Prioridade: P2)

**Objetivo**: permitir ao Master listar usuários paginados e consultar uma conta individual.

**Verificação independente**: resultados contêm somente `id`, `name` e `login`; paginação é respeitada; usuário ausente resulta em 404; autorização Master é aplicada.

### Red — testes primeiro

- [ ] T031 [US3] Escrever em `tests/CadastroUsuarios.IntegrationTests/Features/Users/ReadUsersTests.cs` testes de listagem, paginação, detalhe, 404, 401/403 e ausência de campos internos; executar `dotnet test` e confirmar falhas esperadas.

### Green e Refactor — implementação

- [ ] T032 [US3] Criar `src/CadastroUsuarios.Api/Features/Users/UserPageResponse.cs` com `items`, `page`, `pageSize` e `totalCount`.
- [ ] T033 [US3] Implementar `GET /api/users` em `src/CadastroUsuarios.Api/Features/Users/UserEndpoints.cs` com paginação limitada, projeção pública e consulta somente leitura.
- [ ] T034 [US3] Implementar `GET /api/users/{id}` em `src/CadastroUsuarios.Api/Features/Users/UserEndpoints.cs`, retornando 404 para conta ausente e sem campos internos de Identity.
- [ ] T035 [US3] Mapear e proteger consultas com autorização Master em `src/CadastroUsuarios.Api/Program.cs`; executar testes US3 e refatorar até ficarem verdes.

## Fase 6: História 4 — Editar perfil (Prioridade: P3)

**Objetivo**: permitir atualizar nome e/ou login sem alterar a senha.

**Verificação independente**: consulta posterior reflete os novos dados; duplicidade e usuário ausente são recusados; senha permanece inalterada.

### Red — testes primeiro

- [ ] T036 [US4] Escrever em `tests/CadastroUsuarios.IntegrationTests/Features/Users/UpdateUserTests.cs` testes de edição parcial, login duplicado, formato e tamanho inválidos do login, 404, 401/403 e senha inalterada; executar `dotnet test` e confirmar falhas esperadas.

### Green e Refactor — implementação

- [ ] T037 [US4] Criar `src/CadastroUsuarios.Api/Features/Users/UpdateUserRequest.cs` com campos editáveis `name` e `login`, sem campo de senha.
- [ ] T038 [US4] Implementar `PUT /api/users/{id}` em `src/CadastroUsuarios.Api/Features/Users/UserEndpoints.cs` com atualização Identity, normalização de login, conflito e 404.
- [ ] T039 [US4] Mapear edição com autorização Master e antiforgery em `src/CadastroUsuarios.Api/Program.cs`; executar testes US4 e refatorar até ficarem verdes.

## Fase 7: História 5 — Alterar senha (Prioridade: P3)

**Objetivo**: permitir que Master altere a senha separadamente da edição do perfil.

**Verificação independente**: senha válida é aceita; senha curta e conta ausente são recusadas; resposta não revela senha nem hash.

### Red — testes primeiro

- [ ] T040 [US5] Escrever em `tests/CadastroUsuarios.IntegrationTests/Features/Users/ChangeUserPasswordTests.cs` testes de troca válida, política mínima, 404, 401/403 e resposta sem credenciais; executar `dotnet test` e confirmar falhas esperadas.

### Green e Refactor — implementação

- [ ] T041 [US5] Criar `src/CadastroUsuarios.Api/Features/Users/ChangeUserPasswordRequest.cs` com campo obrigatório `newPassword` e mínimo de 8 caracteres.
- [ ] T042 [US5] Implementar `PUT /api/users/{id}/password` em `src/CadastroUsuarios.Api/Features/Users/UserEndpoints.cs` usando `GeneratePasswordResetTokenAsync` e `ResetPasswordAsync`, retornando 204 sem credencial.
- [ ] T043 [US5] Mapear a troca de senha com autorização Master e antiforgery em `src/CadastroUsuarios.Api/Program.cs`; executar testes US5 e refatorar até ficarem verdes.

## Fase 8: História 6 — Excluir usuário (Prioridade: P4)

**Objetivo**: excluir usuários comuns e preservar ao menos um Master ativo, inclusive sob concorrência.

**Verificação independente**: usuário comum excluído desaparece das consultas; conta ausente resulta em 404; não é possível excluir o último Master, mesmo com exclusões concorrentes.

### Red — testes primeiro

- [ ] T044 [US6] Escrever em `tests/CadastroUsuarios.IntegrationTests/Features/Users/DeleteUserTests.cs` testes de exclusão comum, 404, 401/403, último Master e duas exclusões concorrentes de Masters; executar `dotnet test` e confirmar falhas esperadas.

### Green e Refactor — implementação

- [ ] T045 [US6] Implementar `DELETE /api/users/{id}` em `src/CadastroUsuarios.Api/Features/Users/UserEndpoints.cs` com verificação de existência e exclusão via `UserManager`.
- [ ] T046 [US6] Executar a contagem de membros Master e a exclusão dentro de uma transação `SERIALIZABLE` no mesmo `ApplicationDbContext`, retornando conflito se a conta for a última, em `src/CadastroUsuarios.Api/Features/Users/UserEndpoints.cs`.
- [ ] T047 [US6] Mapear exclusão com autorização Master e antiforgery em `src/CadastroUsuarios.Api/Program.cs`; executar testes US6 e refatorar até ficarem verdes.

## Fase 9: Polimento e alinhamento de contrato

**Objetivo**: manter documentação, comportamento e suíte de testes coerentes.

- [ ] T048 Atualizar `specs/001-user-management/quickstart.md` com comandos efetivos de configuração, migration, bootstrap, testes e verificação manual.
- [ ] T049 Revisar `specs/001-user-management/contracts/openapi.yaml` contra os endpoints implementados e ajustar somente divergências aprovadas.
- [ ] T050 Executar `dotnet test CadastroUsuarios.sln` e revisar o resultado antes de encerrar a implementação.
- [ ] T051 Revisar `src/CadastroUsuarios.Api/Program.cs` e arquivos das features para confirmar ausência de registro público, segredos versionados e exposição de dados de senha.

## Dependências e ordem

- Fase 1 → Fase 2 → US1 → US2 → US3 → US4 → US5 → US6 → Polimento.
- Cada história começa pela fase Red, continua em Green e termina em Refactor; não começar código de produção da história antes de observar a falha do teste correspondente.
- US1 e US2 são o MVP inicial: autenticar Master e cadastrar usuários.
- As histórias ficam sequenciais para esta implementação individual; compartilham `UserEndpoints.cs`, `Program.cs` e a fixture de integração.

## Estratégia TDD

Para cada história: escrever um teste que expresse um cenário de aceitação; executar `dotnet test` e observar a falha esperada; implementar o mínimo necessário; executar a suíte novamente; refatorar mantendo tudo verde. Os testes de integração usam uma connection string exclusiva de SQL Server de teste, nunca dados de produção. Usar testes unitários para lógica isolada quando isso trouxer valor sem substituir testes de integração das regras HTTP e relacionais.
