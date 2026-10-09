-- ==============================================================================
-- SISTEMA: AERO CONCEPTS (SAD PRECIFICAÇÃO)
-- VERIFICAÇÃO DE INTEGRIDADE DOS DADOS (SOMENTE LEITURA)
-- DATA: 09/10/2026
-- STATUS: APENAS SELECT. NÃO ALTERA NADA NO BANCO. PODE SER EXECUTADO A QUALQUER MOMENTO.
-- COMO USAR: execute o script inteiro. A seção 1 devolve uma linha por regra com a quantidade de ocorrências
--            (o esperado é 0 em todas). As seções 2 e 3 listam os registros problemáticos (esperado: 0 linhas)
--            e a seção 4 é apenas informativa.
-- ==============================================================================

USE sad_precificacao;

-- ==============================================================================
-- 1. RESUMO (uma linha por regra; "ocorrencias" deve ser 0)
-- ==============================================================================
SELECT 'Órfão: funcionário com cargo inexistente' AS verificacao, COUNT(*) AS ocorrencias
FROM funcionario f LEFT JOIN cargo c ON c.id_cargo = f.id_cargo WHERE c.id_cargo IS NULL
UNION ALL
SELECT 'Órfão: projeto com cliente inexistente', COUNT(*)
FROM projeto p LEFT JOIN cliente c ON c.id_cliente = p.id_cliente WHERE c.id_cliente IS NULL
UNION ALL
SELECT 'Órfão: projeto com usuário (autor) inexistente', COUNT(*)
FROM projeto p LEFT JOIN usuario u ON u.id_usuario = p.id_usuario WHERE u.id_usuario IS NULL
UNION ALL
SELECT 'Órfão: tarefa com projeto inexistente', COUNT(*)
FROM tarefa t LEFT JOIN projeto p ON p.id_projeto = t.id_projeto WHERE p.id_projeto IS NULL
UNION ALL
SELECT 'Órfão: tarefa com funcionário inexistente', COUNT(*)
FROM tarefa t LEFT JOIN funcionario f ON f.id_funcionario = t.id_funcionario WHERE f.id_funcionario IS NULL
UNION ALL
SELECT 'Órfão: custo com projeto inexistente', COUNT(*)
FROM custo c LEFT JOIN projeto p ON p.id_projeto = c.id_projeto WHERE p.id_projeto IS NULL
UNION ALL
SELECT 'Órfão: custo com item de catálogo inexistente', COUNT(*)
FROM custo c LEFT JOIN catalogo_custo cc ON cc.id_catalogo_custo = c.id_catalogo_custo WHERE cc.id_catalogo_custo IS NULL
UNION ALL
SELECT 'Órfão: orçamento com projeto inexistente', COUNT(*)
FROM orcamento o LEFT JOIN projeto p ON p.id_projeto = o.id_projeto WHERE p.id_projeto IS NULL
UNION ALL
SELECT 'Projeto sem orçamento', COUNT(*)
FROM projeto p LEFT JOIN orcamento o ON o.id_projeto = p.id_projeto WHERE o.id_projeto IS NULL
UNION ALL
SELECT 'Duplicidade: cargos com o mesmo nome', COUNT(*) FROM (
    SELECT 1 FROM cargo GROUP BY LOWER(TRIM(nome)) HAVING COUNT(*) > 1) d
UNION ALL
SELECT 'Duplicidade: clientes com o mesmo CPF/CNPJ (só dígitos)', COUNT(*) FROM (
    SELECT 1 FROM cliente
    WHERE cpf_cnpj IS NOT NULL AND TRIM(cpf_cnpj) <> ''
    GROUP BY REPLACE(REPLACE(REPLACE(REPLACE(TRIM(cpf_cnpj), '.', ''), '-', ''), '/', ''), ' ', '')
    HAVING COUNT(*) > 1) d
UNION ALL
SELECT 'Duplicidade: itens de catálogo com o mesmo nome e categoria', COUNT(*) FROM (
    SELECT 1 FROM catalogo_custo GROUP BY LOWER(TRIM(nome)), IFNULL(categoria, '') HAVING COUNT(*) > 1) d
UNION ALL
SELECT 'Regra: sistema sem nenhum administrador ativo (1 = problema)',
       CASE WHEN COUNT(*) = 0 THEN 1 ELSE 0 END
FROM usuario WHERE nivel = 'Administrador' AND status = 'Ativo'
UNION ALL
SELECT 'Regra: tarefa com horas estimadas negativas', COUNT(*) FROM tarefa WHERE horas_estimadas < 0
UNION ALL
SELECT 'Regra: custo de catálogo com valor negativo', COUNT(*) FROM catalogo_custo WHERE valor < 0
UNION ALL
SELECT 'Regra: cargo com custo médio por hora negativo', COUNT(*) FROM cargo WHERE custo_medio_hora < 0;

-- ==============================================================================
-- 2. DETALHE DOS ÓRFÃOS (cada consulta deve devolver 0 linhas)
-- ==============================================================================

-- 2.1 Funcionários com cargo inexistente
SELECT f.id_funcionario, f.nome, f.id_cargo
FROM funcionario f LEFT JOIN cargo c ON c.id_cargo = f.id_cargo
WHERE c.id_cargo IS NULL;

-- 2.2 Projetos com cliente ou usuário inexistente
SELECT p.id_projeto, p.nome, p.id_cliente, p.id_usuario
FROM projeto p
LEFT JOIN cliente c ON c.id_cliente = p.id_cliente
LEFT JOIN usuario u ON u.id_usuario = p.id_usuario
WHERE c.id_cliente IS NULL OR u.id_usuario IS NULL;

-- 2.3 Tarefas com projeto ou funcionário inexistente
SELECT t.id_tarefa, t.id_projeto, t.id_funcionario
FROM tarefa t
LEFT JOIN projeto p ON p.id_projeto = t.id_projeto
LEFT JOIN funcionario f ON f.id_funcionario = t.id_funcionario
WHERE p.id_projeto IS NULL OR f.id_funcionario IS NULL;

-- 2.4 Custos com projeto ou item de catálogo inexistente
SELECT c.id_custo, c.id_projeto, c.id_catalogo_custo
FROM custo c
LEFT JOIN projeto p ON p.id_projeto = c.id_projeto
LEFT JOIN catalogo_custo cc ON cc.id_catalogo_custo = c.id_catalogo_custo
WHERE p.id_projeto IS NULL OR cc.id_catalogo_custo IS NULL;

-- 2.5 Orçamentos com projeto inexistente
SELECT o.id_orcamento, o.id_projeto
FROM orcamento o LEFT JOIN projeto p ON p.id_projeto = o.id_projeto
WHERE p.id_projeto IS NULL;

-- 2.6 Projetos sem orçamento
SELECT p.id_projeto, p.nome
FROM projeto p LEFT JOIN orcamento o ON o.id_projeto = p.id_projeto
WHERE o.id_projeto IS NULL;

-- ==============================================================================
-- 3. DETALHE DAS DUPLICIDADES (cada consulta deve devolver 0 linhas)
-- ==============================================================================

-- 3.1 Cargos com o mesmo nome
SELECT LOWER(TRIM(nome)) AS nome_normalizado, COUNT(*) AS quantidade, GROUP_CONCAT(id_cargo) AS ids
FROM cargo GROUP BY LOWER(TRIM(nome)) HAVING COUNT(*) > 1;

-- 3.2 Clientes com o mesmo CPF/CNPJ (compara só os dígitos)
SELECT REPLACE(REPLACE(REPLACE(REPLACE(TRIM(cpf_cnpj), '.', ''), '-', ''), '/', ''), ' ', '') AS documento,
       COUNT(*) AS quantidade, GROUP_CONCAT(id_cliente) AS ids
FROM cliente
WHERE cpf_cnpj IS NOT NULL AND TRIM(cpf_cnpj) <> ''
GROUP BY documento HAVING COUNT(*) > 1;

-- 3.3 Itens de catálogo com o mesmo nome e categoria
SELECT LOWER(TRIM(nome)) AS nome_normalizado, categoria, COUNT(*) AS quantidade, GROUP_CONCAT(id_catalogo_custo) AS ids
FROM catalogo_custo GROUP BY LOWER(TRIM(nome)), categoria HAVING COUNT(*) > 1;

-- ==============================================================================
-- 4. INFORMATIVO: usuários e funcionários inativos
-- ==============================================================================

-- 4.1 Administradores ativos (deve haver pelo menos 1)
SELECT id_usuario, nome, email, status FROM usuario WHERE nivel = 'Administrador' ORDER BY status, nome;

-- 4.2 Funcionários inativos que ainda constam em tarefas (permitido: tarefas antigas mantêm o responsável)
SELECT f.id_funcionario, f.nome, f.status, COUNT(t.id_tarefa) AS tarefas
FROM funcionario f JOIN tarefa t ON t.id_funcionario = f.id_funcionario
WHERE f.status <> 'Ativo'
GROUP BY f.id_funcionario, f.nome, f.status;
