# Cadastro de Usuários API

API backend para autenticação administrativa e gerenciamento de usuários. O projeto usa C# com .NET 10, ASP.NET Core Identity, Entity Framework Core e SQL Server. A interface Angular será desenvolvida em uma etapa futura.

## Funcionalidades

- Provisionamento explícito do primeiro usuário Master.
- Login e sessão administrativa usando cookie.
- Cadastro, consulta, edição, troca de senha e exclusão de usuários.
- Proteção das operações administrativas por autorização Master e antiforgery.
- Proteção contra exclusão do último usuário Master.

## Tecnologias

- .NET 10 / C# 14
- ASP.NET Core Minimal APIs
- ASP.NET Core Identity
- Entity Framework Core 10
- SQL Server local para desenvolvimento
- xUnit e testes de integração com SQL Server

## Pré-requisitos

- SDK do .NET 10
- SQL Server instalado e acessível localmente como `localhost`
- PowerShell

Execute os comandos abaixo na raiz do repositório.

## Configurar e executar localmente

Restaure a ferramenta local do Entity Framework:

```powershell
dotnet tool restore
```

Configure a connection string de desenvolvimento com User Secrets, fora dos arquivos versionados:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" 'Server=localhost;Database=CadastroUsuarios;Integrated Security=True;TrustServerCertificate=True' --project src/CadastroUsuarios.Api
```

Aplique as migrations:

```powershell
dotnet ef database update --project src/CadastroUsuarios.Api
```

Configure temporariamente os dados do usuário Master e execute o provisionamento:

```powershell
dotnet user-secrets set "Master:Name" "Nome do administrador" --project src/CadastroUsuarios.Api
dotnet user-secrets set "Master:Login" "login.master" --project src/CadastroUsuarios.Api
dotnet user-secrets set "Master:Password" "substitua-por-uma-senha-segura" --project src/CadastroUsuarios.Api
dotnet run --project src/CadastroUsuarios.Api -- --provision-master
```

Guarde a senha escolhida para usar no login. Depois do provisionamento, remova os valores temporários de bootstrap:

> O provisionador cria o Master se ele ainda não existir e garante sua role. Ele não redefine a senha de uma conta já existente: trocar `Master:Password` no User Secrets não altera o hash salvo no banco. Se o login já existir com outra senha, o provisionamento informa erro. Para uma base local descartável, recrie o banco antes de provisionar; não apague uma base que contenha dados a preservar.

```powershell
dotnet user-secrets remove "Master:Name" --project src/CadastroUsuarios.Api
dotnet user-secrets remove "Master:Login" --project src/CadastroUsuarios.Api
dotnet user-secrets remove "Master:Password" --project src/CadastroUsuarios.Api
```

Inicie a API no perfil HTTP local:

```powershell
dotnet run --project src/CadastroUsuarios.Api --launch-profile http
```

A API estará em `http://localhost:5044`. Antes de iniciá-la, remova `ConnectionStrings__TestDatabase` da sessão do PowerShell caso essa variável tenha sido definida para executar os testes; a aplicação prioriza essa configuração quando ela está presente. O documento OpenAPI gerado pela aplicação, disponível no ambiente Development, estará em `http://localhost:5044/openapi/v1.json`.

## Configuração e arquivos `.env`

O .NET **não carrega arquivos `.env` por padrão**. Este projeto usa os provedores de configuração padrão do ASP.NET Core: `appsettings.json`, `appsettings.{Environment}.json`, User Secrets em desenvolvimento, variáveis de ambiente e argumentos de linha de comando. [Documentação de configuração do ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-10.0)

Para desenvolvimento local, este projeto já tem `UserSecretsId`; use `dotnet user-secrets` para connection strings e credenciais. O Secret Manager mantém os valores fora do repositório, mas não os criptografa e serve somente para desenvolvimento. [Documentação do Secret Manager](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-10.0)

Também é possível fornecer uma configuração temporária por variável de ambiente no PowerShell. O separador `__` representa `:` nas chaves hierárquicas do .NET:

```powershell
$env:ConnectionStrings__DefaultConnection = 'Server=localhost;Database=CadastroUsuarios;Integrated Security=True;TrustServerCertificate=True'
dotnet run --project src/CadastroUsuarios.Api --launch-profile http
```

Essa variável vale para a janela atual do PowerShell. Não salve segredos em `.env`, `appsettings.json` ou no Git.

## Testes automatizados

Os testes de integração usam um banco separado e descartável. Na mesma janela PowerShell em que executará os testes, defina a connection string e rode a solution:

```powershell
$env:ConnectionStrings__TestDatabase = 'Server=localhost;Database=CadastroUsuariosTests;Integrated Security=True;TrustServerCertificate=True'
dotnet test CadastroUsuarios.sln
```

O `ApiFactory` aplica as migrations e limpa as tabelas de Identity. Use somente um banco reservado aos testes: os dados de usuários e roles desse banco são apagados. Ao terminar, remova a variável da sessão:

```powershell
Remove-Item Env:ConnectionStrings__TestDatabase
```

## Testar manualmente no Postman

### Executar uma coleção pelo Postman CLI

O Postman CLI está instalado (`postman --version`). Ele executa uma coleção salva localmente; primeiro importe o OpenAPI conforme as instruções abaixo e salve/exporte a coleção como arquivo JSON. Uma execução por caminho local mostra os resultados no terminal e não exige login no Postman. [Documentação do Postman CLI](https://learning.postman.com/docs/postman-cli/postman-cli-run-collection).

Se exportar também um Environment para um arquivo local, selecione nele `baseUrl`, `masterLogin` e `masterPassword` e rode:

```powershell
postman collection run .\postman\CadastroUsuarios.postman_collection.json --environment .\postman\CadastroUsuarios.local.postman_environment.json
```

Use os nomes dos arquivos que você exportou. Mantenha o Environment local fora do Git, pois ele contém a senha Master. A coleção deve ter os requests na ordem do fluxo abaixo e o script que copia `requestToken` para `antiforgeryToken`; depois do login, chame novamente o endpoint antiforgery antes das operações protegidas. O CLI executa os requests na ordem da coleção e roda seus scripts de teste.

### Importar as rotas

Com a API em execução, importe `http://localhost:5044/openapi/v1.json` no Postman como **Postman Collection**. Também é possível importar o contrato versionado em `specs/001-user-management/contracts/openapi.yaml`. A documentação oficial do Postman explica a importação de OpenAPI e a geração de uma coleção de requests: [Import an API specification](https://learning.postman.com/docs/design-apis/specifications/import-a-specification).

Crie um Environment no Postman com:

| Variável | Valor inicial |
| --- | --- |
| `baseUrl` | `http://localhost:5044` |
| `antiforgeryToken` | vazio; será preenchido pelo request abaixo |
| `masterLogin` | login definido no provisionamento |
| `masterPassword` | senha escolhida no provisionamento |

### Obter o token antiforgery

Crie e envie:

```http
GET {{baseUrl}}/api/auth/antiforgery
```

Na aba **Tests** desse request, salve o token do corpo da resposta no Environment:

```javascript
pm.environment.set("antiforgeryToken", pm.response.json().requestToken);
```

O Postman mantém os cookies recebidos no cookie jar e os envia às chamadas seguintes para o mesmo host. Confira **Cookies** se uma chamada autenticada não receber o cookie de sessão esperado. [Documentação do cookie jar do Postman](https://learning.postman.com/docs/use/send-requests/response-data/cookies).

### Fazer login

Crie um request `POST` para `{{baseUrl}}/api/auth/login` com:

- Header: `RequestVerificationToken: {{antiforgeryToken}}`
- Body → raw → JSON:

```json
{
  "login": "{{masterLogin}}",
  "password": "{{masterPassword}}"
}
```

O resultado esperado é `204 No Content`; o cookie de autenticação fica no cookie jar do Postman. Depois do login, envie `GET {{baseUrl}}/api/auth/antiforgery` novamente para obter um token associado à sessão autenticada; o script da aba **Tests** pode permanecer igual.

### Conferir a sessão e chamar os endpoints

Envie `GET {{baseUrl}}/api/auth/session`; o resultado deve conter `id`, `name` e `login`. Para as próximas chamadas que alteram dados, inclua o header `RequestVerificationToken: {{antiforgeryToken}}`.

| Operação | Método e URL | Resultado esperado |
| --- | --- | --- |
| Listar usuários | `GET {{baseUrl}}/api/users?page=1&pageSize=20` | `200 OK` |
| Consultar usuário | `GET {{baseUrl}}/api/users/{id}` | `200 OK` ou `404 Not Found` |
| Cadastrar usuário | `POST {{baseUrl}}/api/users` | `201 Created` |
| Editar usuário | `PUT {{baseUrl}}/api/users/{id}` | `200 OK` |
| Trocar senha | `PUT {{baseUrl}}/api/users/{id}/password` | `204 No Content` |
| Excluir usuário | `DELETE {{baseUrl}}/api/users/{id}` | `204 No Content`; `409 Conflict` se for o último Master |
| Encerrar sessão | `POST {{baseUrl}}/api/auth/logout` | `204 No Content` |

Exemplo de body para cadastro:

```json
{
  "name": "Usuário de teste",
  "login": "usuario.teste",
  "password": "senha-com-oito-caracteres"
}
```

Use o `id` retornado pelo cadastro nos requests de detalhe, edição, troca de senha e exclusão. Os endpoints de cadastro e consulta não retornam senha nem hash. A autenticação é por cookie, não por Bearer token. O antiforgery também é necessário no login, logout e nas operações de escrita.

## Endpoints disponíveis

| Área | Rotas |
| --- | --- |
| Autenticação | `GET /api/auth/antiforgery`, `POST /api/auth/login`, `GET /api/auth/session`, `POST /api/auth/logout` |
| Usuários | `GET /api/users`, `POST /api/users`, `GET /api/users/{id}`, `PUT /api/users/{id}`, `PUT /api/users/{id}/password`, `DELETE /api/users/{id}` |

As rotas administrativas exigem sessão autenticada com role Master. Não há cadastro público.

## Estrutura e documentação

- `src/CadastroUsuarios.Api/Features/Authentication`: autenticação, sessão e contratos de login.
- `src/CadastroUsuarios.Api/Features/Users`: endpoints e contratos de usuário.
- `src/CadastroUsuarios.Api/Infrastructure/Identity`: usuário Identity e provisionamento Master.
- `src/CadastroUsuarios.Api/Infrastructure/Persistence`: contexto EF Core e migrations.
- `tests/CadastroUsuarios.IntegrationTests`: testes de integração HTTP e SQL Server.
- `specs/001-user-management/quickstart.md`: guia de execução e verificação local.
- `specs/001-user-management/contracts/openapi.yaml`: contrato OpenAPI versionado.
- `AGENTS.md`: convenções de arquitetura, TDD e desenvolvimento em dupla.

## Desenvolvimento

O projeto segue SOLID e TDD. Para cada comportamento, escreva primeiro um teste, confirme a falha esperada, implemente a mudança mínima e execute novamente os testes. As regras que dependem do SQL Server são verificadas com testes de integração relacionais, não com EF Core InMemory.
