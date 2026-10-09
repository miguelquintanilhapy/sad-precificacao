using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using magal.Models;
using magal.Data;

namespace magal.Data.Repositories
{
    public class CustoRepository
    {
        public async Task<List<Custo>> ListarPorProjeto(int idProjeto)
        {
            var lista = new List<Custo>();

            try
            {
                using (var conn = (MySqlConnection)DbConnectionFactory.CreateConnection())
                {
                    await conn.OpenAsync();

                    string sql = @"
                        SELECT 
                            c.id_custo,
                            c.id_projeto,
                            c.id_catalogo_custo,
                            cc.nome,
                            cc.categoria,
                            cc.valor,
                            c.tipo,
                            c.unidade
                        FROM custo c
                        INNER JOIN catalogo_custo cc ON c.id_catalogo_custo = cc.id_catalogo_custo
                        WHERE c.id_projeto = @idProjeto
                        ORDER BY c.id_custo DESC";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@idProjeto", idProjeto);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                lista.Add(new Custo
                                {
                                    id_custo = reader.GetInt32(reader.GetOrdinal("id_custo")),
                                    id_projeto = reader.GetInt32(reader.GetOrdinal("id_projeto")),
                                    id_catalogo_custo = reader.GetInt32(reader.GetOrdinal("id_catalogo_custo")),
                                    nome = reader.GetString(reader.GetOrdinal("nome")),
                                    categoria = reader.IsDBNull(reader.GetOrdinal("categoria")) ? string.Empty : reader.GetString(reader.GetOrdinal("categoria")),
                                    valor = reader.GetDecimal(reader.GetOrdinal("valor")),
                                    tipo = reader.IsDBNull(reader.GetOrdinal("tipo")) ? string.Empty : reader.GetString(reader.GetOrdinal("tipo")),
                                    unidade = reader.IsDBNull(reader.GetOrdinal("unidade")) ? string.Empty : reader.GetString(reader.GetOrdinal("unidade"))
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Erro ao listar os custos do projeto", ex);
            }

            return lista;
        }

        public async Task InserirCustoNoProjeto(int idProjeto, int idCatalogoCusto, string tipo, string unidade)
        {
            try
            {
                using (var conn = (MySqlConnection)DbConnectionFactory.CreateConnection())
                {
                    await conn.OpenAsync();

                    string sql = @"
                        INSERT INTO custo (id_projeto, id_catalogo_custo, tipo, unidade)
                        VALUES (@idProjeto, @idCatalogoCusto, @tipo, @unidade)";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@idProjeto", idProjeto);
                        cmd.Parameters.AddWithValue("@idCatalogoCusto", idCatalogoCusto);
                        cmd.Parameters.AddWithValue("@tipo", tipo ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@unidade", unidade ?? (object)DBNull.Value);

                        await cmd.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Erro ao vincular custo ao projeto", ex);
            }
        }

        public async Task ExcluirCustoDoProjeto(int idCusto)
        {
            try
            {
                using (var conn = (MySqlConnection)DbConnectionFactory.CreateConnection())
                {
                    await conn.OpenAsync();

                    string sql = "DELETE FROM custo WHERE id_custo = @id";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", idCusto);

                        await cmd.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Erro ao excluir custo do projeto", ex);
            }
        }
    }
}