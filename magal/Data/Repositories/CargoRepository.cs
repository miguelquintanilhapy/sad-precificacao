using System;
using System.Collections.Generic;
using System.Threading.Tasks; // Mantido para o async funcionar
using MySql.Data.MySqlClient;
using magal.Models;
using magal.Data;

namespace magal.Data.Repositories
{
    public class CargoRepository
    {
        // Nome mantido como "ListarTodos", mas agora é async Task
        public async Task<List<Cargo>> ListarTodos()
        {
            var lista = new List<Cargo>();

            try
            {
                using (var conn = (MySqlConnection)DbConnectionFactory.CreateConnection())
                {
                    await conn.OpenAsync();

                    string sql = "SELECT id_cargo, nome, custo_medio_hora FROM cargo";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                lista.Add(new Cargo
                                {
                                    id_cargo = reader.GetInt32(reader.GetOrdinal("id_cargo")),
                                    nome = reader.GetString(reader.GetOrdinal("nome")),
                                    custo_medio_hora = reader.GetDecimal(reader.GetOrdinal("custo_medio_hora"))
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Erro no CargoRepository (Listar): " + ex.Message, ex);
            }

            return lista;
        }

        // Nome mantido como "Inserir", mas agora é async Task
        public async Task Inserir(Cargo cargo)
        {
            try
            {
                using (var conn = (MySqlConnection)DbConnectionFactory.CreateConnection())
                {
                    await conn.OpenAsync();

                    string sql = @"
                        INSERT INTO cargo (
                            nome,
                            custo_medio_hora
                        )
                        VALUES
                        (
                            @nome,
                            @custo
                        )";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@nome", cargo.nome);
                        cmd.Parameters.AddWithValue("@custo", cargo.custo_medio_hora);

                        await cmd.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Erro ao inserir cargo: " + ex.Message, ex);
            }
        }

        // Nome mantido como "Atualizar", mas agora é async Task
        public async Task Atualizar(Cargo cargo)
        {
            try
            {
                using (var conn = (MySqlConnection)DbConnectionFactory.CreateConnection())
                {
                    await conn.OpenAsync();

                    string sql = @"
                        UPDATE cargo
                        SET
                            nome = @nome,
                            custo_medio_hora = @custo
                        WHERE id_cargo = @id";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", cargo.id_cargo);
                        cmd.Parameters.AddWithValue("@nome", cargo.nome);
                        cmd.Parameters.AddWithValue("@custo", cargo.custo_medio_hora);

                        await cmd.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Erro ao atualizar cargo: " + ex.Message, ex);
            }
        }

        // Nome mantido como "Excluir", mas agora é async Task
        public async Task Excluir(int idCargo)
        {
            try
            {
                using (var conn = (MySqlConnection)DbConnectionFactory.CreateConnection())
                {
                    await conn.OpenAsync();

                    string sql = "DELETE FROM cargo WHERE id_cargo = @id";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", idCargo);

                        await cmd.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Erro ao excluir cargo: " + ex.Message, ex);
            }
        }
    }
}