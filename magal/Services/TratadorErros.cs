using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using MySql.Data.MySqlClient;

namespace magal.Services
{
    /// <summary>
    /// Ponto único de tratamento de erros: traduz exceções técnicas (MySQL, IO, etc.) em mensagens amigáveis
    /// em português e grava o detalhe técnico num log local. O usuário nunca deve ver ex.Message, SQL,
    /// nome de tabela/constraint ou stack trace.
    /// </summary>
    public static class TratadorErros
    {
        private const string TituloAviso = "Aero Concepts";

        private static readonly object _lockLog = new object();
        private static bool _exibindo;

        /// <summary>Pasta dos logs: %LocalAppData%\Magal\logs (um arquivo por dia).</summary>
        public static string PastaLogs { get; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Magal", "logs");

        /// <summary>
        /// Registra o erro no log e mostra a mensagem amigável. RegraNegocioException aparece como aviso;
        /// qualquer outra exceção, como erro. "acao" completa a frase "Não foi possível ..." (ex.: "excluir o cargo").
        /// </summary>
        public static void Mostrar(Exception ex, string acao)
        {
            Registrar(ex, acao);

            // MessageBox reentra o dispatcher: se outro erro estourar enquanto a caixa está aberta, só registra.
            if (_exibindo) return;

            try
            {
                _exibindo = true;

                string mensagem = ObterMensagemAmigavel(ex, acao);
                bool aviso = Localizar<RegraNegocioException>(ex) != null;

                ExecutarNaInterface(() => MessageBox.Show(
                    mensagem,
                    TituloAviso,
                    MessageBoxButton.OK,
                    aviso ? MessageBoxImage.Warning : MessageBoxImage.Error));
            }
            catch
            {
                // Falha ao exibir a mensagem não pode gerar novo erro para o usuário.
            }
            finally
            {
                _exibindo = false;
            }
        }

        /// <summary>Grava o detalhe técnico no log local (nunca lança exceção).</summary>
        public static void Registrar(Exception ex, string contexto)
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {contexto}");
                sb.AppendLine(Sanitizar(ex?.ToString() ?? "(sem exceção)"));
                sb.AppendLine(new string('-', 60));

                lock (_lockLog)
                {
                    Directory.CreateDirectory(PastaLogs);
                    File.AppendAllText(
                        Path.Combine(PastaLogs, $"{DateTime.Now:yyyy-MM-dd}.log"),
                        sb.ToString(),
                        Encoding.UTF8);
                }
            }
            catch
            {
                // Log é melhor esforço: pasta sem permissão ou disco cheio não pode derrubar o app.
            }
        }

        /// <summary>Monta o texto exibido ao usuário, sem nenhum detalhe técnico.</summary>
        public static string ObterMensagemAmigavel(Exception ex, string contexto)
        {
            var regra = Localizar<RegraNegocioException>(ex);
            if (regra != null) return regra.Message;

            string prefixo = string.IsNullOrWhiteSpace(contexto)
                ? "Não foi possível concluir a operação."
                : $"Não foi possível {contexto}.";

            string causa = DescreverCausa(ex);
            return causa == null
                ? $"{prefixo}\n\nOcorreu um erro inesperado. Tente novamente; se o problema persistir, contate o suporte de TI."
                : $"{prefixo}\n\n{causa}";
        }

        #region Tradução da causa

        private static string DescreverCausa(Exception ex)
        {
            var mysql = Localizar<MySqlException>(ex);
            if (mysql != null) return DescreverMySql(mysql);

            if (Localizar<SocketException>(ex) != null || Localizar<TimeoutException>(ex) != null)
                return "Não foi possível comunicar com o servidor de dados. Verifique a conexão de rede e tente novamente.";

            if (Localizar<UnauthorizedAccessException>(ex) != null)
                return "Sem permissão para gravar o arquivo nesse local. Escolha outra pasta.";

            if (Localizar<IOException>(ex) != null)
                return "Não foi possível acessar o arquivo. Verifique se ele está aberto em outro programa e tente novamente.";

            return null;
        }

        private static string DescreverMySql(MySqlException ex)
        {
            switch (ex.Number)
            {
                case 1451:
                    return "Existem registros vinculados a este item. Remova ou altere esses vínculos antes de excluir.";
                case 1452:
                    return "Um dos itens selecionados não existe mais (cliente, cargo, funcionário ou custo). Atualize a tela e selecione novamente.";
                case 1062:
                    return DescreverDuplicado(ex.Message);
                case 1406:
                    return "Algum campo excede o tamanho máximo permitido. Reduza o texto e tente novamente.";
                case 1048:
                case 1364:
                    return "Preencha todos os campos obrigatórios.";
                case 1264:
                case 1265:
                case 1292:
                case 1366:
                    return "Algum campo contém um valor inválido ou fora do limite permitido.";
                case 1044:
                case 1045:
                    return "Não foi possível acessar o banco de dados com as credenciais configuradas. Contate o suporte de TI.";
                case 1042:
                case 2002:
                case 2003:
                case 2006:
                case 2013:
                    return "Não foi possível conectar ao servidor de dados. Verifique a conexão de rede e tente novamente.";
                case 1205:
                case 1213:
                    return "O banco de dados está ocupado no momento. Aguarde alguns instantes e tente novamente.";
                case 1049:
                case 1146:
                case 1054:
                    return "A estrutura do banco de dados está desatualizada em relação ao sistema. Contate o suporte de TI.";
                default:
                    return null;
            }
        }

        // O texto do erro 1062 traz o nome da chave: "Duplicate entry 'x' for key 'usuario.email'" (MySQL 8)
        // ou "... for key 'email'" (5.7). Usa só o nome da chave para escolher a frase; o valor duplicado não é exibido.
        private static string DescreverDuplicado(string textoErro)
        {
            var m = Regex.Match(textoErro ?? string.Empty, @"for key '([^']+)'", RegexOptions.IgnoreCase);
            string chave = m.Success ? m.Groups[1].Value.ToLowerInvariant() : string.Empty;

            if (chave.Contains("email")) return "Já existe um usuário cadastrado com este e-mail.";
            if (chave.Contains("orcamento") || chave.Contains("id_projeto")) return "Este projeto já possui um orçamento.";

            return "Já existe um registro com estes dados.";
        }

        #endregion

        #region Cadeia de exceções

        /// <summary>
        /// Procura o primeiro T na cadeia de InnerException, entrando também em AggregateException:
        /// os repositórios reempacotam o erro original.
        /// </summary>
        private static T Localizar<T>(Exception ex) where T : Exception
        {
            var pilha = new Stack<Exception>();
            if (ex != null) pilha.Push(ex);

            var visitadas = new HashSet<Exception>();
            while (pilha.Count > 0)
            {
                var atual = pilha.Pop();
                if (!visitadas.Add(atual)) continue;

                if (atual is T achada) return achada;

                if (atual is AggregateException agg)
                    foreach (var interna in agg.InnerExceptions) pilha.Push(interna);
                else if (atual.InnerException != null)
                    pilha.Push(atual.InnerException);
            }

            return null;
        }

        #endregion

        #region Auxiliares

        private static void ExecutarNaInterface(Action acao)
        {
            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher == null || dispatcher.CheckAccess()) acao();
            else dispatcher.Invoke(acao);
        }

        // Garante que credenciais não vão parar no log caso alguma mensagem de erro as repita.
        private static string Sanitizar(string texto)
        {
            return Regex.Replace(texto, @"(Pwd|Password|Uid|User Id|Server|Data Source)\s*=\s*[^;\r\n]*", "$1=***",
                RegexOptions.IgnoreCase);
        }

        #endregion
    }
}
