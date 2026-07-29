using System;
using System.Collections.Generic;
using System.Threading.Tasks; // Adicionado para suportar Task
using magal.Models;
using MySql.Data.MySqlClient;

namespace magal.Data.Repositories
{
    public class FuncionarioRepository
    {
        public async Task<List<Funcionario>> ListarTodos()
        {
            var lista = new List<Funcionario>();

            try
            {
                // Adicionado o cast para (MySqlConnection) para liberar os métodos Async
                using (var conn = (MySqlConnection)DbConnectionFactory.CreateConnection())
                {
                    string query = @"
                        SELECT 
                            f.id_funcionario,
                            f.nome,
                            f.id_cargo,
                            f.nivel,
                            f.tipo_vinculo,
                            f.status,

                            c.nome AS cargo_nome,
                            c.custo_medio_hora
                           

                        FROM funcionario f

                        INNER JOIN cargo c 
                            ON f.id_cargo = c.id_cargo";

                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = query;

                        // Adicionado o await no OpenAsync
                        await conn.OpenAsync();

                        // Adicionado o await no ExecuteReaderAsync
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            // Adicionado o await no ReadAsync
                            while (await reader.ReadAsync())
                            {
                                var func = new Funcionario
                                {
                                    id_funcionario = reader.GetInt32(
                                        reader.GetOrdinal("id_funcionario")),

                                    nome = reader.GetString(
                                        reader.GetOrdinal("nome")),

                                    id_cargo = reader.GetInt32(
                                        reader.GetOrdinal("id_cargo")),

                                    nivel = reader.IsDBNull(reader.GetOrdinal("nivel"))
                                        ? "" : reader.GetString(reader.GetOrdinal("nivel")),

                                    tipo_vinculo = reader.IsDBNull(reader.GetOrdinal("tipo_vinculo"))
                                        ? "" : reader.GetString(reader.GetOrdinal("tipo_vinculo")),

                                    status = reader.IsDBNull(reader.GetOrdinal("status"))
                                        ? "Ativo" : reader.GetString(reader.GetOrdinal("status")),

                                    Cargo = new Cargo
                                    {
                                        id_cargo = reader.GetInt32(
                                            reader.GetOrdinal("id_cargo")),

                                        nome = reader.GetString(
                                            reader.GetOrdinal("cargo_nome")),

                                        custo_medio_hora = reader.GetDecimal(
                                            reader.GetOrdinal("custo_medio_hora"))

                                    }
                                };

                                lista.Add(func);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(
                    "Erro no FuncionarioRepository: " + ex.Message, ex);
            }

            return lista;
        }

        public async Task Inserir(Funcionario funcionario)
        {
            using (var conn =
                   (MySqlConnection)DbConnectionFactory.CreateConnection())
            {
                await conn.OpenAsync();

                string sql = @"
                    INSERT INTO funcionario
                    (
                        id_cargo,
                        nome,
                        nivel,                        
                        tipo_vinculo,
                        status
                    )
                    VALUES
                    (
                        @id_cargo,
                        @nome,
                        @nivel,
                        @tipo_vinculo,
                        @status
                    )";

                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue(
                        "@id_cargo", funcionario.id_cargo);

                    cmd.Parameters.AddWithValue(
                        "@nome", funcionario.nome);

                    cmd.Parameters.AddWithValue(
                        "@nivel", funcionario.nivel);

                    cmd.Parameters.AddWithValue(
                        "@tipo_vinculo", funcionario.tipo_vinculo);

                    cmd.Parameters.AddWithValue(
                        "@status", funcionario.status);

                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task Atualizar(Funcionario funcionario)
        {
            using (var conn =
                   (MySqlConnection)DbConnectionFactory.CreateConnection())
            {
                await conn.OpenAsync();

                string sql = @"
                    UPDATE funcionario
                    SET
                        nome = @nome,
                        id_cargo = @id_cargo,
                        nivel = @nivel,
                        tipo_vinculo = @tipo_vinculo,
                        status = @status
                    WHERE id_funcionario = @id_funcionario";

                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue(
                        "@nome", funcionario.nome);

                    cmd.Parameters.AddWithValue(
                        "@id_cargo", funcionario.id_cargo);

                    cmd.Parameters.AddWithValue(
                        "@nivel", funcionario.nivel);

                    cmd.Parameters.AddWithValue(
                        "@tipo_vinculo", funcionario.tipo_vinculo);

                    cmd.Parameters.AddWithValue(
                        "@status", funcionario.status);

                    cmd.Parameters.AddWithValue(
                        "@id_funcionario",
                        funcionario.id_funcionario);

                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task Excluir(int id_funcionario)
        {
            using (var conn =
                   (MySqlConnection)DbConnectionFactory.CreateConnection())
            {
                await conn.OpenAsync();

                string sql =
                    "DELETE FROM funcionario WHERE id_funcionario = @id";

                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@id", id_funcionario);

                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }
    }
}