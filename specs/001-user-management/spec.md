# Especificação: Gerenciamento de usuários

**Feature Branch**: 001-user-management  
**Criado em**: 2026-11-04  
**Status**: Draft

**Descrição**: Um Master autenticado gerencia usuários com nome, login e senha. Uma interface para consulta será desenvolvida em uma etapa futura.

## Cenários de usuário e testes

### História 1 — Acessar como Master (Prioridade: P1)

Como Master, quero autenticar com meu login e senha para gerenciar usuários.

**Por que esta prioridade**: A autenticação protege os dados e habilita as operações de gerenciamento.

**Teste independente**: Usar as credenciais válidas do Master e confirmar acesso às operações protegidas.

**Cenários de aceitação**:

1. **Dado** o Master provisionado e credenciais válidas, **quando** ele se autentica, **então** pode acessar as operações de gerenciamento.
2. **Dado** credenciais inválidas ou uma solicitação sem autenticação ou privilégio de Master, **quando** ela tenta acessar uma operação de gerenciamento, **então** o acesso é recusado.

---

### História 2 — Cadastrar usuário (Prioridade: P1)

Como Master, quero cadastrar uma pessoa com nome, login e senha para que ela passe a existir no sistema.

**Por que esta prioridade**: O cadastro cria os registros que serão consultados e gerenciados.

**Teste independente**: Cadastrar um usuário válido e confirmar que pode ser localizado.

**Cenários de aceitação**:

1. **Dado** dados válidos e um login não utilizado, **quando** o Master cadastra o usuário, **então** o usuário é criado.
2. **Dado** um login já utilizado, **quando** o Master tenta cadastrar outro usuário com esse login, **então** o cadastro é recusado e o usuário existente permanece inalterado.
3. **Dado** um cadastro sem campo obrigatório ou sem autenticação de Master, **quando** é enviado, **então** nenhum usuário é criado e a solicitação inválida é informada.

---

### História 3 — Consultar usuários (Prioridade: P2)

Como Master, quero listar usuários e consultar um usuário individual para conferir os cadastros e alimentar a futura interface.

**Por que esta prioridade**: A consulta permite verificar os cadastros e fornecer os dados para a tela futura.

**Teste independente**: Listar usuários e consultar um deles individualmente.

**Cenários de aceitação**:

1. **Dado** que há usuários cadastrados, **quando** o Master solicita a lista, **então** recebe os dados sem as senhas.
2. **Dado** um identificador existente ou inexistente, **quando** o Master consulta o usuário, **então** recebe os dados sem a senha ou uma indicação de que não foi encontrado.
3. **Dado** uma solicitação sem privilégio de Master, **quando** tenta consultar usuários, **então** o acesso é recusado.

---

### História 4 — Editar perfil de usuário (Prioridade: P3)

Como Master, quero atualizar nome ou login de um usuário para manter seu perfil correto.

**Por que esta prioridade**: Corrigir os dados do perfil mantém a lista de usuários atualizada.

**Teste independente**: Alterar nome ou login e confirmar a atualização na consulta seguinte.

**Cenários de aceitação**:

1. **Dado** um usuário existente e dados válidos, **quando** o Master edita o perfil, **então** nome e/ou login são atualizados sem alterar a senha.
2. **Dado** um login utilizado por outra pessoa ou um identificador inexistente, **quando** o Master tenta editar, **então** a alteração é recusada e a situação é informada.
3. **Dado** uma solicitação sem privilégio de Master, **quando** tenta editar um perfil, **então** o acesso é recusado.

---

### História 5 — Alterar senha de usuário (Prioridade: P3)

Como Master, quero alterar a senha de um usuário em uma ação separada da edição de perfil.

**Por que esta prioridade**: A separação torna explícita a alteração de uma credencial.

**Teste independente**: Alterar uma senha e confirmar o sucesso da operação sem expor a senha na resposta.

**Cenários de aceitação**:

1. **Dado** um usuário existente e uma nova senha válida, **quando** o Master executa a ação separada, **então** a credencial é atualizada.
2. **Dado** uma solicitação sem privilégio de Master, **quando** tenta alterar uma senha, **então** o acesso é recusado.
3. **Dado** uma alteração concluída, **quando** a operação retorna, **então** a senha não aparece na resposta.

---

### História 6 — Excluir usuário (Prioridade: P4)

Como Master, quero excluir um usuário comum para que ele deixe de aparecer nas consultas.

**Por que esta prioridade**: A exclusão remove cadastros que não devem mais constar na relação.

**Teste independente**: Excluir um usuário comum e confirmar que não aparece mais na lista nem em consulta individual.

**Cenários de aceitação**:

1. **Dado** um usuário comum existente, **quando** o Master o exclui, **então** ele deixa de aparecer nas consultas.
2. **Dado** que há apenas um Master ativo, **quando** uma solicitação tenta excluí-lo ou retirar seu privilégio, **então** a operação é recusada.
3. **Dado** um identificador inexistente ou uma solicitação sem privilégio de Master, **quando** tenta excluir, **então** a operação não ocorre e a situação é informada.

### Casos de borda

- Nome, login ou senha ausentes ou vazios.
- Senha com menos de 8 caracteres.
- Login já utilizado no cadastro ou edição.
- Login vazio, maior que 30 caracteres, com caractere não permitido, ponto no início/fim ou pontos consecutivos.
- Identificador inexistente em consulta, edição ou exclusão.
- Solicitação não autenticada ou sem privilégio de Master.
- Tentativa de excluir ou retirar o privilégio do último Master.
- A senha não aparece em respostas de criação, consulta, listagem, edição ou alteração de senha.

## Requisitos

### Requisitos funcionais

- **FR-001**: O sistema DEVE permitir que o Master se autentique com login e senha.
- **FR-002**: Somente um Master autenticado PODE executar operações de gerenciamento; solicitações sem autenticação ou sem esse privilégio DEVEM ser recusadas.
- **FR-003**: O sistema NÃO DEVE permitir cadastro público de usuários.
- **FR-004**: O primeiro Master DEVE ser provisionado antes do uso das operações de gerenciamento, com credenciais fornecidas fora do controle de versão.
- **FR-005**: O Master DEVE poder cadastrar usuários comuns informando nome, login e senha.
- **FR-006**: Cada login DEVE ser exclusivo, sem distinção entre maiúsculas e minúsculas.
- **FR-007**: O Master DEVE poder listar e consultar usuários individuais sem receber suas senhas.
- **FR-008**: O Master DEVE poder editar nome e login sem alterar a senha nessa operação.
- **FR-009**: O Master DEVE poder alterar a senha de um usuário por uma ação separada.
- **FR-010**: O Master DEVE poder excluir usuários comuns.
- **FR-011**: O sistema NÃO DEVE permitir excluir ou retirar o privilégio do último Master ativo.
- **FR-012**: O sistema DEVE informar quando o usuário consultado, editado ou excluído não existir.
- **FR-013**: O sistema DEVE recusar operações com dados obrigatórios ausentes ou inválidos.
- **FR-014**: O sistema NUNCA DEVE devolver senhas em respostas de operações de usuário.
- **FR-015**: A senha DEVE conter no mínimo 8 caracteres; não há exigência adicional de composição nesta versão.
- **FR-016**: O login DEVE conter de 1 a 30 caracteres, usando somente letras ASCII (`A-Z`, `a-z`), números (`0-9`), ponto (`.`) e sublinhado (`_`). Ponto não pode iniciar ou terminar o login nem aparecer em sequência.

### Entidades principais

- **Usuário**: pessoa registrada com nome, login exclusivo e senha. A conta Master tem privilégio para gerenciar usuários.

## Critérios de sucesso

### Resultados mensuráveis

- **SC-001**: O Master provisionado consegue se autenticar; solicitações sem autenticação ou privilégio de Master não conseguem gerenciar usuários.
- **SC-002**: Todo cadastro aceito pode ser localizado pelo Master na lista e em consulta individual.
- **SC-003**: Após edição de perfil, consultas mostram os dados atualizados e a senha permanece inalterada.
- **SC-004**: Após alteração de senha, a credencial é atualizada e a senha não aparece em respostas.
- **SC-005**: Após excluir um usuário comum, ele não aparece nas consultas; o último Master permanece ativo.
- **SC-006**: Nenhum cadastro ou edição aceita um login já utilizado.

## Premissas

- Nome, login e senha são obrigatórios no cadastro.
- Login identifica exclusivamente um usuário; será comparado sem distinção entre maiúsculas/minúsculas após normalização e remoção de espaços externos. Deve ter de 1 a 30 caracteres: letras ASCII, números, ponto e sublinhado; ponto não pode ficar no início/fim nem aparecer em sequência.
- Usuários criados pelo Master são contas comuns. Não há cadastro público nem autenticação de usuários comuns nesta etapa.
- A troca de senha ocorre em ação separada do Master; a senha nunca aparece em respostas.
- As senhas devem ter no mínimo 8 caracteres. O sistema não exige tipos específicos de caracteres nesta versão.
- O último Master ativo não pode ser excluído nem perder seu privilégio.
- As credenciais iniciais do Master são fornecidas fora do controle de versão; o processo de provisionamento será definido no plano.
- A interface para consulta será especificada em uma etapa futura.
