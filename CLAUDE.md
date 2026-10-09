# Convenções do projeto (magal)

## Erros: nunca mostrar texto técnico ao usuário

- Todo `catch` que informa o usuário usa `TratadorErros.Mostrar(ex, "ação que falhou")`
  (`Services/TratadorErros.cs`). A ação completa a frase "Não foi possível ...", ex.: `"excluir o cargo"`.
- Proibido: `MessageBox.Show(... ex.Message ...)`, `{ex}`, `ex.ToString()` ou stack trace em tela.
  Falhas silenciosas (antes `Debug.WriteLine`) vão para `TratadorErros.Registrar(ex, "contexto")`.
- O detalhe técnico fica no log local `%LocalAppData%\Magal\logs\AAAA-MM-DD.log` (credenciais mascaradas).
- Em repositórios, reempacote preservando a causa: `throw new Exception("contexto", ex)`.
  Nunca `"... " + ex.Message`: isso põe o texto do SQL na mensagem e esconde o número do erro do MySQL.
- Mensagem pronta para o usuário (regra de negócio) = `throw new RegraNegocioException("texto em português")`.
  Aparece como aviso, não como erro. Em `Excluir`/`Inserir`/`Atualizar` com `try/catch`, deixe passar com
  `catch (RegraNegocioException) { throw; }` antes do `catch (Exception)`.
- Novos códigos de erro do MySQL se traduzem em `TratadorErros.DescreverMySql`. O 1062 (duplicado) escolhe a
  frase pelo nome da chave presente no texto do erro.
- Todo `async void` que acessa banco precisa de `try/catch` com `TratadorErros.Mostrar`.
  `App.xaml.cs` tem handlers globais como rede de segurança, não como substituto.

## Integridade de dados

- **Exclusão com vínculos:** o `Excluir` de cada repositório chama `Data/VerificadorVinculos` antes do `DELETE`
  (cargo, cliente, funcionário, usuário, item de catálogo). A mensagem traz contagem e até 5 nomes.
  Projeto não tem verificação: tarefas, custos e orçamento apagam em cascata, de propósito.
  Ao criar uma tabela filha, adicione a regra no verificador.
- **Duplicidade:** `Data/VerificadorDuplicidade` roda em `Inserir`/`Atualizar` (cargo por nome, cliente por
  CPF/CNPJ só dígitos, item de catálogo por nome + categoria). O banco só tem `UNIQUE` em `usuario.email`.
- **Último administrador:** `VerificadorVinculos.GarantirNaoUltimoAdministrador` impede excluir, rebaixar ou
  inativar o único Administrador ativo. O usuário logado também não pode se excluir nem se inativar.
- **Funcionário inativo** não aparece como opção de responsável em tarefa nova, mas continua nas tarefas já
  salvas (`OrcamentoViewModel.GarantirFuncionarioNaLista`).
- Verificações ficam no repositório, não só na tela, para proteger qualquer chamador.
- SQL de verificação é sempre constante e parametrizado (`@id`); nunca concatene texto do usuário.

## `await` obrigatório

- Toda chamada a repositório (`Inserir`, `Atualizar`, `Excluir`, `Listar...`) leva `await`; o handler vira
  `async void` com `try/catch`. Sem `await`, o erro se perde e a tela mostra "sucesso" (ou remove o item da lista)
  mesmo com o banco recusando.
- Confira com grep: `grep -rnE "(repo|_repository)\.\w+\(" --include=*.cs Views ViewModels | grep -v await`
  deve voltar vazio.
- Nos diálogos de edição, o botão Salvar fica desabilitado durante a gravação e, se falhar, o objeto volta aos
  valores originais (`EdicaoSegura.Copiar` / `EdicaoSegura.Restaurar`).

## Banco de dados

- Mudança de banco (DDL/DML) vira arquivo em `magal/Scripts/` e é aplicada manualmente, depois de um dump.
  `Scripts/verificacao_integridade.sql` é somente leitura (`SELECT`) e procura órfãos e duplicidades.
- Todas as chaves estrangeiras reais são `NO ACTION` (bloqueiam exclusão) ou `CASCADE` (só a partir de projeto).
  Evite `ON DELETE SET NULL`: zera vínculos em silêncio.

## Edição de arquivos por script

- Preserve o BOM e as quebras de linha (CRLF) de cada arquivo ao reescrever com Python ou heredoc.
