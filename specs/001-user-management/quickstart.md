# Início rápido: Gerenciamento de usuários

Este guia descreve como a futura implementação será iniciada e verificada localmente. Os comandos são um roteiro para a etapa de implementação; esta fase criou apenas artefatos de planejamento.

## Pré-requisitos

- SDK do .NET 10.
- SQL Server local ou de desenvolvimento acessível.
- Secret Manager habilitado para o projeto da API.
- Ferramenta `dotnet-ef` instalada localmente pelo tool manifest do repositório.

## Fluxo local previsto

1. Criar a solution e o projeto ASP.NET Core Web API conforme a estrutura em `plan.md`; habilitar nullable reference types.
2. Adicionar Identity/EF Core e o provider SQL Server compatível com EF Core 10; registrar `ApplicationDbContext` e configuração de cookie.
3. Configurar uma connection string de desenvolvimento em User Secrets. Não colocar credenciais reais em `appsettings.json` nem no Git.
4. Criar a primeira migration e aplicar ao banco local com `dotnet ef migrations add InitialIdentitySchema` e `dotnet ef database update`.
5. Definir temporariamente login e senha de bootstrap em User Secrets e executar o comando explícito de provisionamento Master. Confirmar que a role e a conta existem; remover os valores de bootstrap em seguida.
6. Iniciar a API, obter token antiforgery e autenticar com o Master. Usar o cookie e token nas chamadas mutáveis.
7. Verificar manualmente: chamadas não autenticadas recebem 401; conta autenticada sem role Master recebe 403; Master cria e consulta usuário; login duplicado é recusado; edição não troca senha; endpoint separado troca senha; respostas nunca contêm senha; usuário comum pode ser excluído; excluir uma conta Master é recusado quando ela for a última.

## TDD durante a implementação

- Para cada comportamento, adicionar primeiro um teste xUnit ou de integração que descreva o resultado esperado.
- Executar `dotnet test` e confirmar que o teste novo falha pelo comportamento ainda não implementado.
- Implementar o menor código que faça o teste passar, executar novamente e refatorar com os testes verdes.
- Configurar `ConnectionStrings__TestDatabase` para um banco SQL Server exclusivo de teste; nunca apontar para banco de produção ou dados pessoais.
- Regras de persistência e concorrência são verificadas usando SQL Server de teste, não EF Core InMemory.
- A cada tarefa de comportamento, executar primeiro o teste recém-escrito e confirmar a falha esperada; só então implementar e repetir `dotnet test` até passar.

## Verificação no ambiente de desenvolvimento

- Revisar migration e confirmar que `NormalizedUserName` tem unicidade no SQL Server.
- Inspecionar respostas HTTP e confirmar que contêm somente campos de contrato, sem hash, security stamp ou senha.
- Confirmar que nenhum endpoint de registro público foi mapeado.
- Para a etapa de produção, gerar e revisar script SQL ou migration bundle; não executar migrations automaticamente em cada instância da API.

## Decisão que precisa estar fechada antes da implementação do frontend

- Origem e estratégia de hospedagem do Angular e da API quando o frontend for iniciado, para fechar CORS, SameSite e entrega do token antiforgery.
