using System;
using MySql.Data.MySqlClient;
using magal.Data;
using magal.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks; // Adicionado para suportar Task

namespace magal.Data.Repositories
{
    public class ProjetoRepository
    {
        public async Task SalvarProjetoCompleto(Projeto projeto, List<Custo> custosExtras)
        {
            using (var conn = (MySqlConnection)DbConnectionFactory.CreateConnection())
            {
                await conn.OpenAsync();

                using (var transaction = await conn.BeginTransactionAsync())
                {
                    try
                    {
                        if (projeto.id_projeto == 0)
                        {
                            if (projeto.id_usuario == 0)
                                throw new InvalidOperationException("Não é possível salvar um projeto sem um usuário autor válido.");

                            // INSERIR NOVO PROJETO
                            using (var cmd = new MySqlCommand(@"
                                INSERT INTO projeto
                                (nome, id_cliente, id_usuario, data_criacao, tipo, status)
                                VALUES
                                (@nome, @idCliente, @idUsuario, @data, @tipo, @status);
                            ", conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@nome", projeto.nome);
                                cmd.Parameters.AddWithValue("@idCliente", projeto.id_cliente);
                                cmd.Parameters.AddWithValue("@idUsuario", projeto.id_usuario);
                                cmd.Parameters.AddWithValue("@data", projeto.data_criacao == DateTime.MinValue ? DateTime.Now : projeto.data_criacao);
                                cmd.Parameters.AddWithValue("@tipo", projeto.tipo ?? "Serviço");
                                cmd.Parameters.AddWithValue("@status", projeto.status ?? "Rascunho");

                                await cmd.ExecuteNonQueryAsync();

                                cmd.CommandText = "SELECT LAST_INSERT_ID();";
                                projeto.id_projeto = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                            }
                        }
                        else
                        {
                            // ATUALIZAR PROJETO
                            using (var cmd = new MySqlCommand(@"
                                UPDATE projeto
                                SET nome = @nome, id_cliente = @idCliente, status = @status, tipo = @tipo
                                WHERE id_projeto = @idProj
                            ", conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@nome", projeto.nome);
                                cmd.Parameters.AddWithValue("@idCliente", projeto.id_cliente);
                                cmd.Parameters.AddWithValue("@status", projeto.status);
                                cmd.Parameters.AddWithValue("@tipo", projeto.tipo);
                                cmd.Parameters.AddWithValue("@idProj", projeto.id_projeto);

                                await cmd.ExecuteNonQueryAsync();
                            }

                            // LIMPAR TAREFAS E CUSTOS ANTIGOS (Convertidos para Execuções Assíncronas)
                            using (var cmdDelTarefa = new MySqlCommand($"DELETE FROM tarefa WHERE id_projeto = {projeto.id_projeto}", conn, transaction))
                            {
                                await cmdDelTarefa.ExecuteNonQueryAsync();
                            }

                            using (var cmdDelCusto = new MySqlCommand($"DELETE FROM custo WHERE id_projeto = {projeto.id_projeto}", conn, transaction))
                            {
                                await cmdDelCusto.ExecuteNonQueryAsync();
                            }
                        }

                        // INSERIR OU ATUALIZAR ORÇAMENTO
                        using (var cmd = new MySqlCommand(@"
                            REPLACE INTO orcamento
                            (id_projeto, custo_base, percentual_impostos, margem_percentual, valor_margem, valor_impostos, valor_final, validade_dias, forma_pagamento, prazo_entrega, observacoes)
                            VALUES
                            (@idProj, @custo, @percImp, @margPerc, @vMarg, @vImp, @final, @validade, @formaPagamento, @prazoEntrega, @obs);
                        ", conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@idProj", projeto.id_projeto);
                            cmd.Parameters.AddWithValue("@custo", projeto.Orcamento.custo_base);
                            cmd.Parameters.AddWithValue("@percImp", projeto.Orcamento.percentual_impostos);
                            cmd.Parameters.AddWithValue("@margPerc", projeto.Orcamento.margem_percentual);
                            cmd.Parameters.AddWithValue("@vMarg", projeto.Orcamento.valor_margem);
                            cmd.Parameters.AddWithValue("@vImp", projeto.Orcamento.valor_impostos);
                            cmd.Parameters.AddWithValue("@final", projeto.Orcamento.valor_final);
                            cmd.Parameters.AddWithValue("@validade", projeto.Orcamento.validade_dias);

                            cmd.Parameters.AddWithValue("@formaPagamento", projeto.Orcamento.forma_pagamento ?? string.Empty);
                            cmd.Parameters.AddWithValue("@prazoEntrega", projeto.Orcamento.prazo_entrega.HasValue ? (object)projeto.Orcamento.prazo_entrega.Value : DBNull.Value);
                            cmd.Parameters.AddWithValue("@obs", projeto.Orcamento.observacoes ?? string.Empty);

                            await cmd.ExecuteNonQueryAsync();
                        }

                        // INSERIR TAREFAS
                        foreach (var tarefa in projeto.Tarefas)
                        {
                            using (var cmd = new MySqlCommand(@"
                                INSERT INTO tarefa
                                (id_projeto, descricao, id_funcionario, horas_estimadas, status)
                                VALUES
                                (@idProj, @desc, @idFunc, @horas, @status);
                            ", conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@idProj", projeto.id_projeto);
                                cmd.Parameters.AddWithValue("@desc", tarefa.descricao);
                                cmd.Parameters.AddWithValue("@idFunc", tarefa.id_funcionario);
                                cmd.Parameters.AddWithValue("@horas", Convert.ToDecimal(tarefa.horas_estimadas));
                                cmd.Parameters.AddWithValue("@status", tarefa.status ?? "Pendente");

                                await cmd.ExecuteNonQueryAsync();
                            }
                        }

                        // INSERIR CUSTOS
                        foreach (var custo in custosExtras)
                        {
                            using (var cmd = new MySqlCommand(@"
                                INSERT INTO custo
                                (id_projeto, nome, categoria, tipo, valor, unidade, id_catalogo_custo)
                                VALUES
                                (@idProj, @nome, @cat, @tipo, @valor, @unidade, @idCatalogo);
                            ", conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@idProj", projeto.id_projeto);
                                cmd.Parameters.AddWithValue("@nome", custo.nome);
                                cmd.Parameters.AddWithValue("@cat", custo.categoria);
                                cmd.Parameters.AddWithValue("@tipo", custo.tipo ?? "Direto");
                                cmd.Parameters.AddWithValue("@valor", custo.valor);
                                cmd.Parameters.AddWithValue("@unidade", custo.unidade ?? "Unitário");
                                cmd.Parameters.AddWithValue("@idCatalogo", custo.id_catalogo_custo);
                                await cmd.ExecuteNonQueryAsync();
                            }
                        }

                        await transaction.CommitAsync();
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        throw new Exception("Erro ao processar transação no MySQL: " + ex.Message);
                    }
                }
            }
        }

        public async Task<Projeto> CarregarProjetoCompleto(int idProjeto)
        {
            var projeto = new Projeto();

            using (var conn = (MySqlConnection)DbConnectionFactory.CreateConnection())
            {
                await conn.OpenAsync();

                string sqlProj = @"
                    SELECT
                        p.*,
                        o.custo_base,
                        o.percentual_impostos,
                        o.margem_percentual,
                        o.valor_margem,
                        o.valor_impostos,
                        o.valor_final,
                        o.validade_dias,
                        o.forma_pagamento,
                        o.prazo_entrega,
                        o.observacoes
                    FROM projeto p
                    LEFT JOIN orcamento o
                        ON p.id_projeto = o.id_projeto
                    WHERE p.id_projeto = @id
                ";

                using (var cmd = new MySqlCommand(sqlProj, conn))
                {
                    cmd.Parameters.AddWithValue("@id", idProjeto);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            projeto.id_projeto = idProjeto;
                            projeto.nome = reader["nome"].ToString();
                            projeto.id_cliente = Convert.ToInt32(reader["id_cliente"]);
                            projeto.id_usuario = Convert.ToInt32(reader["id_usuario"]);
                            projeto.data_criacao = Convert.ToDateTime(reader["data_criacao"]);
                            projeto.status = reader["status"].ToString();
                            projeto.tipo = reader["tipo"].ToString();

                            projeto.Orcamento = new Orcamento
                            {
                                custo_base = reader["custo_base"] != DBNull.Value ? Convert.ToDecimal(reader["custo_base"]) : 0,
                                percentual_impostos = reader["percentual_impostos"] != DBNull.Value ? Convert.ToDecimal(reader["percentual_impostos"]) : 0,
                                margem_percentual = reader["margem_percentual"] != DBNull.Value ? Convert.ToDecimal(reader["margem_percentual"]) : 0,
                                valor_margem = reader["valor_margem"] != DBNull.Value ? Convert.ToDecimal(reader["valor_margem"]) : 0,
                                valor_impostos = reader["valor_impostos"] != DBNull.Value ? Convert.ToDecimal(reader["valor_impostos"]) : 0,
                                valor_final = reader["valor_final"] != DBNull.Value ? Convert.ToDecimal(reader["valor_final"]) : 0,
                                validade_dias = reader["validade_dias"] != DBNull.Value ? Convert.ToInt32(reader["validade_dias"]) : 15,

                                forma_pagamento = reader["forma_pagamento"] != DBNull.Value ? reader["forma_pagamento"].ToString() : string.Empty,
                                prazo_entrega = reader["prazo_entrega"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(reader["prazo_entrega"]) : null,
                                observacoes = reader["observacoes"] != DBNull.Value ? reader["observacoes"].ToString() : string.Empty
                            };
                        }
                    }
                }

                // TAREFAS
                projeto.Tarefas = new ObservableCollection<Tarefa>();

                string sqlTarefas = @"
                    SELECT *
                    FROM tarefa
                    WHERE id_projeto = @id
                ";

                using (var cmd = new MySqlCommand(sqlTarefas, conn))
                {
                    cmd.Parameters.AddWithValue("@id", idProjeto);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            projeto.Tarefas.Add(new Tarefa
                            {
                                id_tarefa = Convert.ToInt32(reader["id_tarefa"]),
                                descricao = reader["descricao"].ToString(),
                                horas_estimadas = Convert.ToDecimal(reader["horas_estimadas"]),
                                id_funcionario = Convert.ToInt32(reader["id_funcionario"]),
                                status = reader["status"].ToString()
                            });
                        }
                    }
                }

                // CUSTOS
                projeto.Custos = new ObservableCollection<Custo>();

                string sqlCustos = @"
                    SELECT *
                    FROM custo
                    WHERE id_projeto = @id
                ";

                using (var cmd = new MySqlCommand(sqlCustos, conn))
                {
                    cmd.Parameters.AddWithValue("@id", idProjeto);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            projeto.Custos.Add(new Custo
                            {
                                id_custo = Convert.ToInt32(reader["id_custo"]),
                                nome = reader["nome"].ToString(),
                                categoria = reader["categoria"].ToString(),
                                tipo = reader["tipo"].ToString(),
                                valor = Convert.ToDecimal(reader["valor"]),
                                unidade = reader["unidade"].ToString()
                            });
                        }
                    }
                }
            }

            return projeto;
        }

        public Task<List<Projeto>> BuscarTodosPorUsuario(int idUsuario) => BuscarProjetos(idUsuario);

        /// <summary>
        /// Retorna os projetos de todos os usuários, sem filtro de autor (uso restrito a Administrador).
        /// </summary>
        public Task<List<Projeto>> BuscarTodos() => BuscarProjetos(null);

        private async Task<List<Projeto>> BuscarProjetos(int? idUsuario)
        {
            var lista = new List<Projeto>();

            using (var conn = (MySqlConnection)DbConnectionFactory.CreateConnection())
            {
                await conn.OpenAsync();

                string sql = @"
                    SELECT
                        p.*,
                        c.nome AS nome_cliente,
                        o.custo_base,
                        o.margem_percentual,
                        o.percentual_impostos,
                        o.valor_margem,
                        o.valor_impostos,
                        o.valor_final,
                        o.validade_dias,
                        o.forma_pagamento,
                        o.prazo_entrega,
                        o.observacoes
                    FROM projeto p
                    INNER JOIN cliente c
                        ON p.id_cliente = c.id_cliente
                    LEFT JOIN orcamento o
                        ON p.id_projeto = o.id_projeto
                    " + (idUsuario.HasValue ? "WHERE p.id_usuario = @idUser" : "") + @"
                    ORDER BY p.data_criacao DESC
                ";

                using (var cmd = new MySqlCommand(sql, conn))
                {
                    if (idUsuario.HasValue)
                        cmd.Parameters.AddWithValue("@idUser", idUsuario.Value);

                    // Adicionado await no ExecuteReaderAsync
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        // Adicionado await no ReadAsync
                        while (await reader.ReadAsync())
                        {
                            var projeto = new Projeto
                            {
                                id_projeto = Convert.ToInt32(reader["id_projeto"]),
                                nome = reader["nome"].ToString(),
                                id_cliente = Convert.ToInt32(reader["id_cliente"]),
                                data_criacao = Convert.ToDateTime(reader["data_criacao"]),
                                status = reader["status"].ToString(),
                                tipo = reader["tipo"].ToString(),

                                Cliente = new Cliente
                                {
                                    nome = reader["nome_cliente"].ToString()
                                }
                            };

                            if (reader["valor_final"] != DBNull.Value)
                            {
                                projeto.Orcamento = new Orcamento
                                {
                                    custo_base = Convert.ToDecimal(reader["custo_base"]),
                                    margem_percentual = Convert.ToDecimal(reader["margem_percentual"]),
                                    percentual_impostos = Convert.ToDecimal(reader["percentual_impostos"]),
                                    valor_margem = reader["valor_margem"] != DBNull.Value ? Convert.ToDecimal(reader["valor_margem"]) : 0,
                                    valor_impostos = reader["valor_impostos"] != DBNull.Value ? Convert.ToDecimal(reader["valor_impostos"]) : 0,
                                    valor_final = Convert.ToDecimal(reader["valor_final"]),
                                    validade_dias = reader["validade_dias"] != DBNull.Value ? Convert.ToInt32(reader["validade_dias"]) : 15,

                                    forma_pagamento = reader["forma_pagamento"] != DBNull.Value ? reader["forma_pagamento"].ToString() : string.Empty,
                                    prazo_entrega = reader["prazo_entrega"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(reader["prazo_entrega"]) : null,
                                    observacoes = reader["observacoes"] != DBNull.Value ? reader["observacoes"].ToString() : string.Empty
                                };
                            }
                            else
                            {
                                projeto.Orcamento = new Orcamento
                                {
                                    valor_final = 0,
                                    valor_margem = 0,
                                    validade_dias = 15,
                                    forma_pagamento = string.Empty,
                                    prazo_entrega = null,
                                    observacoes = string.Empty
                                };
                            }

                            lista.Add(projeto);
                        }
                    }
                }
            }

            return lista;
        }

        public async Task ExcluirProjeto(int idProjeto)
        {
            using (var conn = (MySqlConnection)DbConnectionFactory.CreateConnection())
            {
                await conn.OpenAsync();

                string sql = @"
                    DELETE FROM projeto
                    WHERE id_projeto = @id
                ";

                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@id", idProjeto);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }
    }
}