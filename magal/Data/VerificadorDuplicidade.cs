using System.Text.RegularExpressions;
using MySql.Data.MySqlClient;
using magal.Services;

namespace magal.Data
{
    /// <summary>
    /// Impede cadastros duplicados que o banco não barra (só usuario.email tem UNIQUE). Roda dentro de
    /// Inserir/Atualizar, antes de gravar. "id" é o registro sendo editado (0 ao inserir), para não
    /// acusar duplicidade contra ele mesmo. SQL constante e parametrizado, somente leitura.
    /// </summary>
    internal static class VerificadorDuplicidade
    {
        internal static async Task Cargo(MySqlConnection conn, int id, string nome)
        {
            if (string.IsNullOrWhiteSpace(nome)) return;

            string existente = await BuscarAsync(conn,
                "SELECT nome FROM cargo WHERE LOWER(TRIM(nome)) = LOWER(TRIM(@nome)) AND id_cargo <> @id LIMIT 1",
                id, ("@nome", nome));

            if (existente != null)
                throw new RegraNegocioException($"Já existe um cargo chamado '{existente}'.");
        }

        internal static async Task Cliente(MySqlConnection conn, int id, string cpfCnpj)
        {
            string digitos = Regex.Replace(cpfCnpj ?? string.Empty, "[^0-9]", string.Empty);
            if (digitos.Length == 0) return;

            // Compara só os dígitos, para "12.345.678/0001-90" e "12345678000190" contarem como o mesmo documento.
            string existente = await BuscarAsync(conn,
                "SELECT nome FROM cliente " +
                "WHERE REPLACE(REPLACE(REPLACE(REPLACE(TRIM(cpf_cnpj), '.', ''), '-', ''), '/', ''), ' ', '') = @doc " +
                "AND id_cliente <> @id LIMIT 1",
                id, ("@doc", digitos));

            if (existente != null)
                throw new RegraNegocioException($"Já existe um cliente cadastrado com este CPF/CNPJ: '{existente}'.");
        }

        internal static async Task ItemCatalogo(MySqlConnection conn, int id, string nome, string categoria)
        {
            if (string.IsNullOrWhiteSpace(nome)) return;

            string existente = await BuscarAsync(conn,
                "SELECT nome FROM catalogo_custo " +
                "WHERE LOWER(TRIM(nome)) = LOWER(TRIM(@nome)) AND IFNULL(categoria, '') = @categoria " +
                "AND id_catalogo_custo <> @id LIMIT 1",
                id, ("@nome", nome), ("@categoria", categoria ?? string.Empty));

            if (existente != null)
                throw new RegraNegocioException(
                    $"Já existe o item '{existente}'" +
                    (string.IsNullOrWhiteSpace(categoria) ? "." : $" na categoria '{categoria}'."));
        }

        private static async Task<string> BuscarAsync(
            MySqlConnection conn, string sql, int id, params (string nome, object valor)[] parametros)
        {
            using (var cmd = new MySqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@id", id);
                foreach (var (nome, valor) in parametros)
                    cmd.Parameters.AddWithValue(nome, valor);

                return (await cmd.ExecuteScalarAsync())?.ToString();
            }
        }
    }
}
