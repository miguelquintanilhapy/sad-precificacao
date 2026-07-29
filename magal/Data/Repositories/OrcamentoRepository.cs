using System;
using MySql.Data.MySqlClient;
using magal.Data;
using magal.Models;
using System.Collections.Generic;
using System.Threading.Tasks; // Adicionado para suportar Task

namespace magal.Data.Repositories
{
    public class OrcamentoRepository
    {
        /// <summary>
        /// Salva ou atualiza os dados financeiros e termos comerciais de um orçamento de forma assíncrona.
        /// </summary>
        public async Task SalvarOrcamento(Orcamento orcamento, int idProjeto)
        {
            using (var conn = (MySqlConnection)DbConnectionFactory.CreateConnection())
            {
                // Adicionado await no OpenAsync
                await conn.OpenAsync();

                // ON DUPLICATE KEY UPDATE em vez de REPLACE INTO, para preservar id_orcamento e data_criacao
                string sql = @"
                    INSERT INTO orcamento
                    (id_projeto, custo_base, percentual_impostos, margem_percentual,
                     valor_margem, valor_impostos, valor_final, validade_dias,
                     forma_pagamento, prazo_entrega, observacoes)
                    VALUES
                    (@idProj, @custo, @percImp, @margPerc,
                     @vMarg, @vImp, @final, @validade,
                     @formaPagto, @prazoEntr, @obs)
                    ON DUPLICATE KEY UPDATE
                        custo_base = VALUES(custo_base),
                        percentual_impostos = VALUES(percentual_impostos),
                        margem_percentual = VALUES(margem_percentual),
                        valor_margem = VALUES(valor_margem),
                        valor_impostos = VALUES(valor_impostos),
                        valor_final = VALUES(valor_final),
                        validade_dias = VALUES(validade_dias),
                        forma_pagamento = VALUES(forma_pagamento),
                        prazo_entrega = VALUES(prazo_entrega),
                        observacoes = VALUES(observacoes);
                ";

                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@idProj", idProjeto);
                    cmd.Parameters.AddWithValue("@custo", orcamento.custo_base);
                    cmd.Parameters.AddWithValue("@percImp", orcamento.percentual_impostos);
                    cmd.Parameters.AddWithValue("@margPerc", orcamento.margem_percentual); 
                    cmd.Parameters.AddWithValue("@vMarg", orcamento.valor_margem);
                    cmd.Parameters.AddWithValue("@vImp", orcamento.valor_impostos);
                    cmd.Parameters.AddWithValue("@final", orcamento.valor_final);
                    cmd.Parameters.AddWithValue("@validade", orcamento.validade_dias);

                    cmd.Parameters.AddWithValue("@formaPagto", (object)orcamento.forma_pagamento ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@prazoEntr", (object)orcamento.prazo_entrega ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@obs", (object)orcamento.observacoes ?? DBNull.Value);

                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        /// <summary>
        /// Busca o orçamento atrelado a um determinado projeto de forma assíncrona.
        /// </summary>
        public async Task<Orcamento> BuscarPorProjeto(int idProjeto)
        {
            using (var conn = (MySqlConnection)DbConnectionFactory.CreateConnection())
            {
                await conn.OpenAsync();

                string sql = @"
                    SELECT id_orcamento, id_projeto, custo_base, percentual_impostos, 
                           margem_percentual, valor_margem, valor_impostos, valor_final, 
                           validade_dias, forma_pagamento, prazo_entrega, observacoes, data_criacao
                    FROM orcamento
                    WHERE id_projeto = @idProj
                ";

                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@idProj", idProjeto);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return new Orcamento
                            {
                                id_orcamento = Convert.ToInt32(reader["id_orcamento"]),
                                data_criacao = Convert.ToDateTime(reader["data_criacao"]),

                                custo_base = reader["custo_base"] != DBNull.Value ? Convert.ToDecimal(reader["custo_base"]) : 0,
                                percentual_impostos = reader["percentual_impostos"] != DBNull.Value ? Convert.ToDecimal(reader["percentual_impostos"]) : 0,
                                margem_percentual = reader["margem_percentual"] != DBNull.Value ? Convert.ToDecimal(reader["margem_percentual"]) : 0,
                                valor_margem = reader["valor_margem"] != DBNull.Value ? Convert.ToDecimal(reader["valor_margem"]) : 0,
                                valor_impostos = reader["valor_impostos"] != DBNull.Value ? Convert.ToDecimal(reader["valor_impostos"]) : 0,
                                valor_final = reader["valor_final"] != DBNull.Value ? Convert.ToDecimal(reader["valor_final"]) : 0,
                                validade_dias = reader["validade_dias"] != DBNull.Value ? Convert.ToInt32(reader["validade_dias"]) : 15,

                                // Mapeamento dos novos campos comerciais
                                forma_pagamento = reader["forma_pagamento"] != DBNull.Value ? reader["forma_pagamento"].ToString() : string.Empty,
                                prazo_entrega = reader["prazo_entrega"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(reader["prazo_entrega"]) : null,
                                observacoes = reader["observacoes"] != DBNull.Value ? reader["observacoes"].ToString() : string.Empty
                            };
                        }
                    }
                }
            }

            return null; 
        }
    }
}