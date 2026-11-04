# Modelo de dados: Gerenciamento de usuários

## ApplicationUser

Usuário da aplicação baseado no modelo de ASP.NET Core Identity (`IdentityUser`), com propriedade adicional de nome. O login da API corresponde ao `UserName` do Identity.

| Campo | Tipo lógico | Regras |
|---|---|---|
| Id | string | Identificador criado pelo Identity; usado nas rotas de usuário. |
| Name | string | Obrigatório; nome exibido nos contratos da API. |
| UserName | string | Obrigatório; de 1 a 30 caracteres ASCII: letras, números, ponto ou sublinhado. Ponto não pode iniciar/terminar nem aparecer em sequência. Espaços externos são removidos; comparação sem distinção de caixa após normalização. |
| NormalizedUserName | string | Derivado pelo normalizador do Identity; índice único garante unicidade no armazenamento. |
| PasswordHash | string? | Gerenciado pelo Identity; nunca exposto em contratos ou logs. Nunca recebe senha em texto puro. |
| SecurityStamp / ConcurrencyStamp | string | Gerenciados pelo Identity para segurança e concorrência. |

## Papel (Role)

Usar `IdentityRole` e relacionamento de usuário/papel do Identity. Papel `Master` é criado pelo comando de bootstrap. Os usuários cadastrados pelo Master não recebem esse papel. Nesta versão, a API não oferece operação para adicionar/remover roles; uma conta Master pode ser excluída somente se outra conta Master ativa permanecer.

## Relacionamentos e invariantes

- Um usuário pode possuir roles conforme o modelo do Identity; a primeira versão usa apenas `Master` no fluxo administrativo.
- Login único após normalização, garantido por índice único no banco além da validação da aplicação.
- Não há tabela ou campo próprio para senha em texto puro.
- Exclusão de usuário comum apaga a conta do Identity; uma exclusão Master deve preservar ao menos uma conta Master ativa, inclusive em operações concorrentes.
- Respostas usam DTOs com `id`, `name` e `login` somente. Hash e demais campos internos não atravessam a API.

## Política de senha

A senha deve ter no mínimo 8 caracteres; não há exigência de caixa, dígitos ou símbolos nesta versão. Configurar o Identity para refletir explicitamente essa política. Persistir somente o hash gerado pelo Identity; hash é uma transformação unidirecional para verificação, não uma senha cifrada para recuperação.
