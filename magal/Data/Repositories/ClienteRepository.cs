using System;
using System.Collections.Generic;
using System.Threading.Tasks; 
using MySql.Data.MySqlClient;
using magal.Models;
using magal.Services;
using magal.Data;

namespace magal.Data.Repositories
{
    public class ClienteRepository
    {
        public async Task<List<Cliente>> ListarTodos()
        {
            var lista = new List<Cliente>();

            try
            {
                using (var conn = (MySqlConnection)DbConnectionFactory.CreateConnection())
                {
                    await conn.OpenAsync();

                    string sql = @"SELECT id_cliente, 
                                          nome, 
                                          tipo, 
                                          cpf_cnpj, 
                                          cidade, 
                                          estado, 
                                          contato 
                                   FROM cliente";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                lista.Add(new Cliente
                                {
                                    id_cliente = reader.GetInt32(reader.GetOrdinal("id_cliente")),

                                    nome = reader.IsDBNull(reader.GetOrdinal("nome"))
                                           ? "" : reader.GetString(reader.GetOrdinal("nome")),

                                    tipo = reader.IsDBNull(reader.GetOrdinal("tipo"))
                                           ? "" : reader.GetString(reader.GetOrdinal("tipo")),

                                    cpf_cnpj = reader.IsDBNull(reader.GetOrdinal("cpf_cnpj"))
                                               ? "" : reader.GetString(reader.GetOrdinal("cpf_cnpj")),

                                    cidade = reader.IsDBNull(reader.GetOrdinal("cidade"))
                                             ? "" : reader.GetString(reader.GetOrdinal("cidade")),

                                    estado = reader.IsDBNull(reader.GetOrdinal("estado"))
                                             ? "" : reader.GetString(reader.GetOrdinal("estado")),

                                    contato = reader.IsDBNull(reader.GetOrdinal("contato"))
                                              ? "" : reader.GetString(reader.GetOrdinal("contato"))
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Erro no ClienteRepository", ex);
            }

            return lista;
        }

        public async Task Inserir(Cliente cliente)
        {
            try
            {
                using (var conn = (MySqlConnection)DbConnectionFactory.CreateConnection())
                {
                    await conn.OpenAsync();
                    await VerificadorDuplicidade.Cliente(conn, 0, cliente.cpf_cnpj);

                    string sql = @"
                        INSERT INTO cliente (
                            nome,
                            tipo,
                            cpf_cnpj,
                            cidade,
                            estado,
                            contato
                        )
                        VALUES
                        (
                            @nome,
                            @tipo,
                            @cpf_cnpj,
                            @cidade,
                            @estado,
                            @contato
                        )";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@nome", cliente.nome ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@tipo", cliente.tipo ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@cpf_cnpj", cliente.cpf_cnpj ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@cidade", cliente.cidade ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@estado", cliente.estado ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@contato", cliente.contato ?? (object)DBNull.Value);

                        await cmd.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (RegraNegocioException) { throw; }
            catch (Exception ex)
            {
                throw new Exception("Erro ao inserir cliente", ex);
            }
        }

        public async Task Atualizar(Cliente cliente)
        {
            try
            {
                using (var conn = (MySqlConnection)DbConnectionFactory.CreateConnection())
                {
                    await conn.OpenAsync();
                    await VerificadorDuplicidade.Cliente(conn, cliente.id_cliente, cliente.cpf_cnpj);

                    string sql = @"
                        UPDATE cliente
                        SET
                            nome = @nome,
                            tipo = @tipo,
                            cpf_cnpj = @cpf_cnpj,
                            cidade = @cidade,
                            estado = @estado,
                            contato = @contato
                        WHERE id_cliente = @id";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", cliente.id_cliente);
                        cmd.Parameters.AddWithValue("@nome", cliente.nome ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@tipo", cliente.tipo ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@cpf_cnpj", cliente.cpf_cnpj ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@cidade", cliente.cidade ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@estado", cliente.estado ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@contato", cliente.contato ?? (object)DBNull.Value);

                        await cmd.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (RegraNegocioException) { throw; }
            catch (Exception ex)
            {
                throw new Exception("Erro ao atualizar cliente", ex);
            }
        }

        public async Task Excluir(int idCliente)
        {
            try
            {
                using (var conn = (MySqlConnection)DbConnectionFactory.CreateConnection())
                {
                    await conn.OpenAsync();
                    await VerificadorVinculos.Cliente(conn, idCliente);

                    string sql = "DELETE FROM cliente WHERE id_cliente = @id";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", idCliente);

                        await cmd.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (RegraNegocioException) { throw; }
            catch (Exception ex)
            {
                throw new Exception("Erro ao excluir cliente", ex);
            }
        }
    }
}