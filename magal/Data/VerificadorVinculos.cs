using System.Text;
using MySql.Data.MySqlClient;
using magal.Services;

namespace magal.Data
{
    /// <summary>
    /// Impede a exclusão de registros que ainda têm vínculos, com uma mensagem clara (contagem + alguns nomes).
    /// Roda dentro de cada Excluir do repositório, antes do DELETE, para proteger qualquer chamador.
    /// Todo o SQL aqui é constante e parametrizado (@id) e somente leitura.
    /// </summary>
    internal static class VerificadorVinculos
    {
        /// <summary>
        /// Um tipo de vínculo: rótulo da contagem (ex.: "funcionário(s)"), SQL que conta e SQL que devolve
        /// até 5 nomes de exemplo (uma coluna). "prefixoExemplos" antecede os nomes, ex.: "nos projetos: ".
        /// </summary>
        internal sealed class Consulta
        {
            public Consulta(string rotulo, string sqlContagem, string sqlExemplos, string prefixoExemplos = "")
            {
                Rotulo = rotulo;
                SqlContagem = sqlContagem;
                SqlExemplos = sqlExemplos;
                PrefixoExemplos = prefixoExemplos;
            }

            public string Rotulo { get; }
            public string SqlContagem { get; }
            public string SqlExemplos { get; }
            public string PrefixoExemplos { get; }
        }

        /// <summary>
        /// Lança RegraNegocioException se o registro "id" tiver qualquer vínculo listado em "consultas".
        /// "descricao" completa "o cargo", "o cliente"...; "sqlNome" devolve o nome do registro a partir de @id.
        /// </summary>
        internal static async Task GarantirSemVinculos(
            MySqlConnection conn, int id, string descricao, string sqlNome, params Consulta[] consultas)
        {
            var vinculos = new List<string>();

            foreach (var consulta in consultas)
            {
                long total = await ContarAsync(conn, consulta.SqlContagem, id);
                if (total == 0) continue;

                var exemplos = await ListarExemplosAsync(conn, consulta.SqlExemplos, id);
                string nomes = exemplos.Count == 0
                    ? string.Empty
                    : $" ({consulta.PrefixoExemplos}{string.Join(", ", exemplos)}{(total > exemplos.Count ? ", ..." : string.Empty)})";

                vinculos.Add($"• {total} {consulta.Rotulo}{nomes}");
            }

            if (vinculos.Count == 0) return;

            string nome = await ObterNomeAsync(conn, sqlNome, id);
            var sb = new StringBuilder();
            sb.Append($"Não é possível excluir {descricao}");
            if (!string.IsNullOrWhiteSpace(nome)) sb.Append($" '{nome}'");
            sb.AppendLine(" porque existem registros vinculados:");
            sb.AppendLine();
            sb.AppendLine(string.Join("\n", vinculos));
            sb.AppendLine();
            sb.Append("Remova ou altere esses vínculos antes de excluir.");

            throw new RegraNegocioException(sb.ToString());
        }

        /// <summary>
        /// O sistema não pode ficar sem nenhum Administrador ativo: bloqueia excluir, rebaixar ou inativar o único.
        /// "acaoImpedida" completa "Não é possível ... o único administrador" (ex.: "excluir").
        /// Só vale se o usuário hoje é Administrador ativo; quem chama decide se ele deixará de ser.
        /// </summary>
        internal static async Task GarantirNaoUltimoAdministrador(MySqlConnection conn, int idUsuario, string acaoImpedida)
        {
            long ehAdminAtivo;
            using (var cmd = new MySqlCommand(
                "SELECT COUNT(*) FROM usuario WHERE id_usuario = @id AND nivel = 'Administrador' AND status = 'Ativo'", conn))
            {
                cmd.Parameters.AddWithValue("@id", idUsuario);
                ehAdminAtivo = Convert.ToInt64(await cmd.ExecuteScalarAsync());
            }
            if (ehAdminAtivo == 0) return;

            long outros = await ContarAsync(conn,
                "SELECT COUNT(*) FROM usuario WHERE nivel = 'Administrador' AND status = 'Ativo' AND id_usuario <> @id", idUsuario);

            if (outros == 0)
                throw new RegraNegocioException(
                    $"Não é possível {acaoImpedida} o único administrador ativo do sistema.\n\n" +
                    "Promova outro usuário a Administrador antes.");
        }

        #region Regras por entidade

        internal static Task Cargo(MySqlConnection conn, int idCargo) => GarantirSemVinculos(
            conn, idCargo, "o cargo",
            "SELECT nome FROM cargo WHERE id_cargo = @id",
            new Consulta("funcionário(s)",
                "SELECT COUNT(*) FROM funcionario WHERE id_cargo = @id",
                "SELECT nome FROM funcionario WHERE id_cargo = @id ORDER BY nome LIMIT 5"));

        internal static Task Cliente(MySqlConnection conn, int idCliente) => GarantirSemVinculos(
            conn, idCliente, "o cliente",
            "SELECT nome FROM cliente WHERE id_cliente = @id",
            new Consulta("projeto(s)",
                "SELECT COUNT(*) FROM projeto WHERE id_cliente = @id",
                "SELECT nome FROM projeto WHERE id_cliente = @id ORDER BY nome LIMIT 5"));

        internal static Task Funcionario(MySqlConnection conn, int idFuncionario) => GarantirSemVinculos(
            conn, idFuncionario, "o funcionário",
            "SELECT nome FROM funcionario WHERE id_funcionario = @id",
            new Consulta("tarefa(s)",
                "SELECT COUNT(*) FROM tarefa WHERE id_funcionario = @id",
                "SELECT DISTINCT p.nome FROM tarefa t JOIN projeto p ON p.id_projeto = t.id_projeto " +
                "WHERE t.id_funcionario = @id ORDER BY p.nome LIMIT 5",
                "nos projetos: "));

        internal static Task Usuario(MySqlConnection conn, int idUsuario) => GarantirSemVinculos(
            conn, idUsuario, "o usuário",
            "SELECT nome FROM usuario WHERE id_usuario = @id",
            new Consulta("projeto(s) criado(s) por ele",
                "SELECT COUNT(*) FROM projeto WHERE id_usuario = @id",
                "SELECT nome FROM projeto WHERE id_usuario = @id ORDER BY nome LIMIT 5"));

        internal static Task ItemCatalogo(MySqlConnection conn, int idCatalogoCusto) => GarantirSemVinculos(
            conn, idCatalogoCusto, "o item de custo",
            "SELECT nome FROM catalogo_custo WHERE id_catalogo_custo = @id",
            new Consulta("custo(s) lançado(s)",
                "SELECT COUNT(*) FROM custo WHERE id_catalogo_custo = @id",
                "SELECT DISTINCT p.nome FROM custo c JOIN projeto p ON p.id_projeto = c.id_projeto " +
                "WHERE c.id_catalogo_custo = @id ORDER BY p.nome LIMIT 5",
                "nos projetos: "));

        #endregion

        #region Acesso ao banco (somente leitura)

        private static async Task<long> ContarAsync(MySqlConnection conn, string sql, int id)
        {
            using (var cmd = new MySqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@id", id);
                return Convert.ToInt64(await cmd.ExecuteScalarAsync());
            }
        }

        private static async Task<List<string>> ListarExemplosAsync(MySqlConnection conn, string sql, int id)
        {
            var lista = new List<string>();
            using (var cmd = new MySqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@id", id);
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        if (!reader.IsDBNull(0)) lista.Add(reader.GetString(0));
                    }
                }
            }
            return lista;
        }

        private static async Task<string> ObterNomeAsync(MySqlConnection conn, string sql, int id)
        {
            using (var cmd = new MySqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@id", id);
                return (await cmd.ExecuteScalarAsync())?.ToString();
            }
        }

        #endregion
    }
}
