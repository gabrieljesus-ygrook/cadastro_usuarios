# Início rápido: API de cadastro de usuários

Este guia registra os comandos para configurar, executar e verificar o backend localmente no Windows, usando PowerShell e SQL Server Express LocalDB.

## Pré-requisitos

- SDK do .NET 10.
- SQL Server Express LocalDB instalado. A instância usada neste guia é `(localdb)\MSSQLLocalDB`.
- Secret Manager habilitado para o projeto da API.
- Docker não é necessário para este fluxo local.

Execute os comandos a partir da raiz do repositório.

## Preparar ferramentas e conexão da API

Restaure a ferramenta `dotnet-ef` registrada no manifesto local:

```powershell
dotnet tool restore
```

Configure a conexão da API em User Secrets. A migration cria o banco `CadastroUsuarios` se ainda não existir:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" 'Server=(localdb)\MSSQLLocalDB;Database=CadastroUsuarios;Integrated Security=True;TrustServerCertificate=True' --project src/CadastroUsuarios.Api
```

Não coloque connection strings com credenciais nem outros segredos em `appsettings.json` ou no Git.

## Criar ou atualizar o banco

As migrations não são aplicadas automaticamente no início normal da API. Aplique-as explicitamente:

```powershell
dotnet ef database update --project src/CadastroUsuarios.Api
```

## Provisionar o primeiro Master

Defina temporariamente os dados do primeiro Master em User Secrets. Use uma senha com pelo menos 8 caracteres:

```powershell
dotnet user-secrets set "Master:Name" "Nome do administrador" --project src/CadastroUsuarios.Api
dotnet user-secrets set "Master:Login" "login.master" --project src/CadastroUsuarios.Api
dotnet user-secrets set "Master:Password" "substitua-por-uma-senha-segura" --project src/CadastroUsuarios.Api
```

Execute o provisionamento explícito:

```powershell
dotnet run --project src/CadastroUsuarios.Api -- --provision-master
```

Depois de confirmar o provisionamento, remova os três valores de bootstrap. A connection string da API permanece configurada:

```powershell
dotnet user-secrets remove "Master:Name" --project src/CadastroUsuarios.Api
dotnet user-secrets remove "Master:Login" --project src/CadastroUsuarios.Api
dotnet user-secrets remove "Master:Password" --project src/CadastroUsuarios.Api
```

## Executar a API

Inicie o perfil HTTP local, que escuta em `http://localhost:5044`:

```powershell
dotnet run --project src/CadastroUsuarios.Api --launch-profile http
```

Mantenha esse terminal aberto. As chamadas abaixo devem ser feitas em outro terminal PowerShell.

## Executar testes de integração

Os testes usam outro banco. Defina a variável de ambiente na mesma janela PowerShell em que executará `dotnet test`:

```powershell
$env:ConnectionStrings__TestDatabase = 'Server=(localdb)\MSSQLLocalDB;Database=CadastroUsuariosTests;Integrated Security=True;TrustServerCertificate=True'
dotnet test tests/CadastroUsuarios.IntegrationTests/CadastroUsuarios.IntegrationTests.csproj
```

`ApiFactory` aplica as migrations e limpa as tabelas de Identity. Portanto, use exclusivamente um banco descartável para os testes; eles apagam usuários e roles desse banco.

Para executar somente os testes da exclusão de usuários:

```powershell
dotnet test tests/CadastroUsuarios.IntegrationTests/CadastroUsuarios.IntegrationTests.csproj --filter FullyQualifiedName~DeleteUserTests
```

Ao terminar, remova a variável daquela janela:

```powershell
Remove-Item Env:ConnectionStrings__TestDatabase
```

## Verificação manual da API

O fluxo abaixo mantém cookies de antiforgery e autenticação com uma sessão PowerShell. Busque um token antes do login e outro depois de autenticar. As rotas que alteram dados exigem o cabeçalho `RequestVerificationToken`.

```powershell
$baseUrl = 'http://localhost:5044'
$session = [Microsoft.PowerShell.Commands.WebRequestSession]::new()

$antiforgery = Invoke-RestMethod "$baseUrl/api/auth/antiforgery" -WebSession $session
$headers = @{ RequestVerificationToken = $antiforgery.requestToken }
$loginBody = @{ login = 'login.master'; password = 'senha-configurada-no-bootstrap' } | ConvertTo-Json
Invoke-RestMethod "$baseUrl/api/auth/login" -Method Post -WebSession $session -Headers $headers -ContentType 'application/json' -Body $loginBody

$antiforgery = Invoke-RestMethod "$baseUrl/api/auth/antiforgery" -WebSession $session
$headers = @{ RequestVerificationToken = $antiforgery.requestToken }
Invoke-RestMethod "$baseUrl/api/auth/session" -WebSession $session

$createBody = @{ name = 'Usuário de teste'; login = 'usuario.teste'; password = 'senha-com-oito-caracteres' } | ConvertTo-Json
$created = Invoke-RestMethod "$baseUrl/api/users" -Method Post -WebSession $session -Headers $headers -ContentType 'application/json' -Body $createBody
Invoke-RestMethod "$baseUrl/api/users?page=1&pageSize=20" -WebSession $session
Invoke-RestMethod "$baseUrl/api/users/$($created.id)" -WebSession $session

$updateBody = @{ name = 'Nome atualizado' } | ConvertTo-Json
Invoke-RestMethod "$baseUrl/api/users/$($created.id)" -Method Put -WebSession $session -Headers $headers -ContentType 'application/json' -Body $updateBody

$passwordBody = @{ newPassword = 'nova-senha-com-oito-caracteres' } | ConvertTo-Json
Invoke-RestMethod "$baseUrl/api/users/$($created.id)/password" -Method Put -WebSession $session -Headers $headers -ContentType 'application/json' -Body $passwordBody
Invoke-RestMethod "$baseUrl/api/users/$($created.id)" -Method Delete -WebSession $session -Headers $headers
```

As respostas de cadastro e consulta expõem somente `id`, `name` e `login`. O endpoint de exclusão retorna `204`; a exclusão do último Master deve retornar `409 Conflict`. O cookie de sessão é mantido pelo `WebRequestSession` e não deve ser copiado para logs ou compartilhado.

## Práticas de desenvolvimento e produção

- Para cada comportamento, escreva primeiro um teste, confirme a falha esperada, implemente a menor mudança e rode os testes novamente.
- Verifique que as migrations mantêm a unicidade de `NormalizedUserName` e que respostas não expõem senha, hash ou dados internos do Identity.
- Não aplique migrations automaticamente no startup normal da API. Para produção, gere e revise um script SQL ou migration bundle antes da aplicação.
- Defina CORS, SameSite e a origem do Angular quando o frontend for iniciado; este repositório contém somente o backend nesta etapa.
