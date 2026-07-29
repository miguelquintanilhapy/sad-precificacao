using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using magal.Models;
using magal.Data;

namespace magal.Data.Repositories
{
    public class UsuarioRepository
    {
        public async Task<List<Usuario>> ListarTodos()
        {
            var lista = new List<Usuario>();

            try
            {
                using (var conn = (MySqlConnection)DbConnectionFactory.CreateConnection())
                {
                    await conn.OpenAsync();

                    string sql = @"SELECT id_usuario, 
                                          nome, 
                                          email, 
                                          senha, 
                                          status,
                                          nivel
                                   FROM usuario 
                                   ORDER BY nome ASC";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                lista.Add(new Usuario
                                {
                                    id_usuario = reader.GetInt32(reader.GetOrdinal("id_usuario")),

                                    nome = reader.IsDBNull(reader.GetOrdinal("nome"))
                                           ? "" : reader.GetString(reader.GetOrdinal("nome")),

                                    email = reader.IsDBNull(reader.GetOrdinal("email"))
                                            ? "" : reader.GetString(reader.GetOrdinal("email")),

                                    senha = reader.IsDBNull(reader.GetOrdinal("senha"))
                                            ? "" : reader.GetString(reader.GetOrdinal("senha")),

                                    status = reader.IsDBNull(reader.GetOrdinal("status"))
                                             ? "Ativo" : reader.GetString(reader.GetOrdinal("status")),

                                    nivel = reader.IsDBNull(reader.GetOrdinal("nivel"))
                                             ? "Operador" : reader.GetString(reader.GetOrdinal("nivel"))
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Erro no UsuarioRepository ao listar: " + ex.Message, ex);
            }

            return lista;
        }

        public async Task Inserir(Usuario usuario)
        {
            try
            {
                using (var conn = (MySqlConnection)DbConnectionFactory.CreateConnection())
                {
                    await conn.OpenAsync();

                    string sql = @"
                        INSERT INTO usuario (
                            nome,
                            email,
                            senha,
                            status,
                            nivel
                        )
                        VALUES
                        (
                            @nome,
                            @email,
                            @senha,
                            @status,
                            @nivel
                        )";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@nome", usuario.nome ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@email", usuario.email ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@senha", usuario.senha ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@status", usuario.status ?? "Ativo");
                        cmd.Parameters.AddWithValue("@nivel", usuario.nivel ?? "Operador");

                        await cmd.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Erro ao inserir usuário: " + ex.Message, ex);
            }
        }

        public async Task @Atualizar(Usuario usuario)
        {
            try
            {
                using (var conn = (MySqlConnection)DbConnectionFactory.CreateConnection())
                {
                    await conn.OpenAsync();

                    string sql = @"
                        UPDATE usuario
                        SET
                            nome = @nome,
                            email = @email,
                            senha = @senha,
                            status = @status,
                            nivel = @nivel
                        WHERE id_usuario = @id";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", usuario.id_usuario);
                        cmd.Parameters.AddWithValue("@nome", usuario.nome ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@email", usuario.email ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@senha", usuario.senha ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@status", usuario.status ?? "Ativo");
                        cmd.Parameters.AddWithValue("@nivel", usuario.nivel ?? "Operador");

                        await cmd.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Erro ao atualizar usuário: " + ex.Message, ex);
            }
        }

        public async Task<Usuario> BuscarPorEmailAtivo(string email)
        {
            try
            {
                using (var conn = (MySqlConnection)DbConnectionFactory.CreateConnection())
                {
                    await conn.OpenAsync();

                    string sql = @"SELECT id_usuario, nome, email, senha, status, nivel
                                   FROM usuario
                                   WHERE email = @email AND status = 'Ativo'";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@email", email);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (!await reader.ReadAsync())
                                return null;

                            return new Usuario
                            {
                                id_usuario = reader.GetInt32(reader.GetOrdinal("id_usuario")),

                                nome = reader.IsDBNull(reader.GetOrdinal("nome"))
                                       ? "" : reader.GetString(reader.GetOrdinal("nome")),

                                email = reader.IsDBNull(reader.GetOrdinal("email"))
                                        ? "" : reader.GetString(reader.GetOrdinal("email")),

                                senha = reader.IsDBNull(reader.GetOrdinal("senha"))
                                        ? "" : reader.GetString(reader.GetOrdinal("senha")),

                                status = reader.IsDBNull(reader.GetOrdinal("status"))
                                         ? "Ativo" : reader.GetString(reader.GetOrdinal("status")),

                                nivel = reader.IsDBNull(reader.GetOrdinal("nivel"))
                                         ? "Operador" : reader.GetString(reader.GetOrdinal("nivel"))
                            };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Erro no UsuarioRepository ao buscar por e-mail: " + ex.Message, ex);
            }
        }

        public async Task AtualizarSenha(int idUsuario, string novoHashSenha)
        {
            try
            {
                using (var conn = (MySqlConnection)DbConnectionFactory.CreateConnection())
                {
                    await conn.OpenAsync();

                    string sql = "UPDATE usuario SET senha = @senha WHERE id_usuario = @id";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@senha", novoHashSenha);
                        cmd.Parameters.AddWithValue("@id", idUsuario);

                        await cmd.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Erro ao atualizar senha do usuário: " + ex.Message, ex);
            }
        }

        public async Task Excluir(int idUsuario)
        {
            try
            {
                using (var conn = (MySqlConnection)DbConnectionFactory.CreateConnection())
                {
                    await conn.OpenAsync();

                    string sql = "DELETE FROM usuario WHERE id_usuario = @id";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", idUsuario);

                        await cmd.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Erro ao excluir usuário: " + ex.Message, ex);
            }
        }
    }
}