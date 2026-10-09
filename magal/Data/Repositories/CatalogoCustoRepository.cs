using System;
using System.Collections.Generic;
using System.Threading.Tasks; 
using MySql.Data.MySqlClient;
using magal.Models;
using magal.Services;
using magal.Data;

namespace magal.Data.Repositories
{
    public class CatalogoCustoRepository
    {
        // Busca apenas as categorias únicas para o primeiro ComboBox
        public async Task<List<string>> ListarCategoriasUnicas()
        {
            var lista = new List<string>();
            try
            {
                using (var conn = (MySqlConnection)DbConnectionFactory.CreateConnection())
                {
                    await conn.OpenAsync();
                    string sql = "SELECT DISTINCT categoria FROM catalogo_custo WHERE categoria IS NOT NULL AND categoria != '' ORDER BY categoria ASC";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                lista.Add(reader.GetString(0));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Erro ao listar categorias únicas do catálogo", ex);
            }
            return lista;
        }

        // Busca os itens específicos da categoria escolhida para o segundo ComboBox
        public async Task<List<CatalogoCusto>> ListarItensPorCategoria(string categoria)
        {
            var lista = new List<CatalogoCusto>();
            try
            {
                using (var conn = (MySqlConnection)DbConnectionFactory.CreateConnection())
                {
                    await conn.OpenAsync();
                    string sql = "SELECT id_catalogo_custo, nome, categoria, valor FROM catalogo_custo WHERE categoria = @categoria ORDER BY nome ASC";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@categoria", categoria);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                lista.Add(new CatalogoCusto
                                {
                                    id_catalogo_custo = reader.GetInt32(reader.GetOrdinal("id_catalogo_custo")),
                                    nome = reader.GetString(reader.GetOrdinal("nome")),
                                    categoria = reader.IsDBNull(reader.GetOrdinal("categoria")) ? string.Empty : reader.GetString(reader.GetOrdinal("categoria")),
                                    valor = reader.GetDecimal(reader.GetOrdinal("valor"))
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Erro ao filtrar itens por categoria", ex);
            }
            return lista;
        }

        public async Task<List<CatalogoCusto>> ListarTodos()
        {
            var lista = new List<CatalogoCusto>();
            try
            {
                using (var conn = (MySqlConnection)DbConnectionFactory.CreateConnection())
                {
                    await conn.OpenAsync();
                    string sql = "SELECT id_catalogo_custo, nome, categoria, valor FROM catalogo_custo ORDER BY id_catalogo_custo DESC";
                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                lista.Add(new CatalogoCusto
                                {
                                    id_catalogo_custo = reader.GetInt32(reader.GetOrdinal("id_catalogo_custo")),
                                    nome = reader.GetString(reader.GetOrdinal("nome")),
                                    categoria = reader.IsDBNull(reader.GetOrdinal("categoria")) ? string.Empty : reader.GetString(reader.GetOrdinal("categoria")),
                                    valor = reader.GetDecimal(reader.GetOrdinal("valor"))
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex) { throw new Exception("Erro ao listar catálogo", ex); }
            return lista;
        }

        public async Task Inserir(CatalogoCusto custo)
        {
            try
            {
                using (var conn = (MySqlConnection)DbConnectionFactory.CreateConnection())
                {
                    await conn.OpenAsync();
                    await VerificadorDuplicidade.ItemCatalogo(conn, 0, custo.nome, custo.categoria);
                    string sql = "INSERT INTO catalogo_custo (nome, categoria, valor) VALUES (@nome, @categoria, @valor)";
                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@nome", custo.nome);
                        cmd.Parameters.AddWithValue("@categoria", custo.categoria ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@valor", custo.valor);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (RegraNegocioException) { throw; }
            catch (Exception ex) { throw new Exception("Erro ao inserir no catálogo", ex); }
        }

        public async Task Atualizar(CatalogoCusto custo)
        {
            try
            {
                using (var conn = (MySqlConnection)DbConnectionFactory.CreateConnection())
                {
                    await conn.OpenAsync();
                    await VerificadorDuplicidade.ItemCatalogo(conn, custo.id_catalogo_custo, custo.nome, custo.categoria);
                    string sql = "UPDATE catalogo_custo SET nome = @nome, categoria = @categoria, valor = @valor WHERE id_catalogo_custo = @id";
                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", custo.id_catalogo_custo);
                        cmd.Parameters.AddWithValue("@nome", custo.nome);
                        cmd.Parameters.AddWithValue("@categoria", custo.categoria ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@valor", custo.valor);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (RegraNegocioException) { throw; }
            catch (Exception ex) { throw new Exception("Erro ao atualizar catálogo", ex); }
        }

        public async Task Excluir(int idCatalogoCusto)
        {
            try
            {
                using (var conn = (MySqlConnection)DbConnectionFactory.CreateConnection())
                {
                    await conn.OpenAsync();
                    await VerificadorVinculos.ItemCatalogo(conn, idCatalogoCusto);
                    string sql = "DELETE FROM catalogo_custo WHERE id_catalogo_custo = @id";
                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", idCatalogoCusto);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (RegraNegocioException) { throw; }
            catch (Exception ex) { throw new Exception("Erro ao excluir do catálogo", ex); }
        }
    }
}