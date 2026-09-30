-- ==============================================================================
-- SISTEMA: AERO CONCEPTS (SAD PRECIFICAÇÃO)
-- ESTRUTURA DO BANCO DE DADOS E CARGA INICIAL (ESPELHO DO BANCO sad_precificacao)
-- DATA: 29/09/2026
-- STATUS: ALINHADO COM O BANCO REAL (ESTRUTURA E DADOS). REEXECUTÁVEL (IF NOT EXISTS / INSERT IGNORE)
-- OBS: custo/hora do funcionário é calculado no app: base do cargo (Pleno) x nível
--      (Júnior /1,75 | Pleno x1 | Sênior x1,5 | Especialista x2).
-- ==============================================================================

CREATE DATABASE IF NOT EXISTS sad_precificacao;
USE sad_precificacao;

-- ==============================================================================
-- 1. CRIAÇÃO DAS TABELAS
-- ==============================================================================

CREATE TABLE IF NOT EXISTS usuario (
    id_usuario INT AUTO_INCREMENT PRIMARY KEY,
    nome VARCHAR(255) NOT NULL,
    email VARCHAR(255) NOT NULL UNIQUE,
    senha VARCHAR(255) NOT NULL,
    status VARCHAR(50) DEFAULT 'Ativo',
    nivel VARCHAR(50) DEFAULT 'Operador'
);

CREATE TABLE IF NOT EXISTS cliente (
    id_cliente INT AUTO_INCREMENT PRIMARY KEY,
    nome VARCHAR(255) NOT NULL,
    tipo VARCHAR(50), 
    cpf_cnpj VARCHAR(20),
    cidade VARCHAR(100),
    estado VARCHAR(50),
    contato VARCHAR(100)
);

CREATE TABLE IF NOT EXISTS cargo (
    id_cargo INT AUTO_INCREMENT PRIMARY KEY,
    nome VARCHAR(255) NOT NULL,
    custo_medio_hora DECIMAL(18, 2) NOT NULL
);

CREATE TABLE IF NOT EXISTS funcionario (
    id_funcionario INT AUTO_INCREMENT PRIMARY KEY,
    id_cargo INT NOT NULL,
    nome VARCHAR(255) NOT NULL,
    nivel VARCHAR(50), 
    tipo_vinculo VARCHAR(50), 
    status VARCHAR(50) DEFAULT 'Ativo',
    FOREIGN KEY (id_cargo) REFERENCES cargo(id_cargo)
);

CREATE TABLE IF NOT EXISTS projeto (
    id_projeto INT AUTO_INCREMENT PRIMARY KEY,
    id_usuario INT NOT NULL,
    id_cliente INT NOT NULL,
    nome VARCHAR(255) NOT NULL,
    tipo VARCHAR(100), 
    status VARCHAR(50) DEFAULT 'Rascunho', 
    data_criacao DATETIME DEFAULT CURRENT_TIMESTAMP,
    data_conclusao_prevista DATE,
    FOREIGN KEY (id_usuario) REFERENCES usuario(id_usuario),
    FOREIGN KEY (id_cliente) REFERENCES cliente(id_cliente)
);

CREATE TABLE IF NOT EXISTS tarefa (
    id_tarefa INT AUTO_INCREMENT PRIMARY KEY,
    id_projeto INT NOT NULL,
    id_funcionario INT NOT NULL,
    descricao VARCHAR(255),
    horas_estimadas DECIMAL(18, 2) DEFAULT 0,
    horas_reais DECIMAL(18, 2) DEFAULT 0,
    custo_real DECIMAL(18, 2) DEFAULT 0,
    status VARCHAR(50) DEFAULT 'Pendente', 
    FOREIGN KEY (id_projeto) REFERENCES projeto(id_projeto) ON DELETE CASCADE,
    FOREIGN KEY (id_funcionario) REFERENCES funcionario(id_funcionario)
);

CREATE TABLE IF NOT EXISTS catalogo_custo (
    id_catalogo_custo INT AUTO_INCREMENT PRIMARY KEY,
    nome VARCHAR(255) NOT NULL,
    categoria VARCHAR(100) NOT NULL,
    valor DECIMAL(18, 2) NOT NULL
);

CREATE TABLE IF NOT EXISTS custo (
    id_custo INT AUTO_INCREMENT PRIMARY KEY,
    id_projeto INT NOT NULL,
    id_catalogo_custo INT NOT NULL, -- Alterado para NOT NULL para garantir o vínculo inverso
    nome VARCHAR(255) NOT NULL,
    categoria VARCHAR(100),
    tipo VARCHAR(50), 
    valor DECIMAL(18, 2) NOT NULL,
    unidade VARCHAR(50), 
    data_cadastro DATETIME DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_custo_projeto FOREIGN KEY (id_projeto) REFERENCES projeto(id_projeto) ON DELETE CASCADE,
    CONSTRAINT fk_custo_catalogo FOREIGN KEY (id_catalogo_custo) REFERENCES catalogo_custo(id_catalogo_custo) ON DELETE NO ACTION
);

CREATE TABLE IF NOT EXISTS orcamento (
    id_orcamento INT AUTO_INCREMENT PRIMARY KEY,
    id_projeto INT NOT NULL UNIQUE, 
    custo_base DECIMAL(18, 2) DEFAULT 0,
    percentual_impostos DECIMAL(18, 2) DEFAULT 0,
    valor_impostos DECIMAL(18, 2) DEFAULT 0,
    margem_percentual DECIMAL(18, 2) DEFAULT 0,
    valor_margem DECIMAL(18, 2) DEFAULT 0,
    valor_final DECIMAL(18, 2) DEFAULT 0,
    validade_dias INT DEFAULT 15,

    -- NOVOS CAMPOS PARA PROFISSIONALIZAR O PDF --
    forma_pagamento VARCHAR(150) NULL,
    prazo_entrega DATE NULL, -- ALTERADO: De VARCHAR(50) para DATE
    observacoes TEXT NULL,
    

    data_criacao DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (id_projeto) REFERENCES projeto(id_projeto) ON DELETE CASCADE
);

-- ==============================================================================
-- 2. CARGA DE DADOS INICIAIS (CONFIGURAÇÕES DO SISTEMA E USUÁRIOS)
-- ==============================================================================

INSERT IGNORE INTO cargo (id_cargo, nome, custo_medio_hora) VALUES
(1, 'Engenheiro Elétrico', 110.00),
(2, 'Supervisor Eletroeletrônico', 85.00),
(3, 'Engenheiro Especialista Turbomáquinas', 120.00),
(4, 'Coordenador Técnico de Serviços', 100.00),
(5, 'Analista de Engenharia Industrial', 70.00),
(6, 'Coordenador de Engenharia Industrial', 100.00),
(7, 'Analista de PD&I', 75.00),
(8, 'Engenheiro de PD&I', 120.00),
(9, 'Gerente de Engenharia', 170.00),
(10, 'Gerente de Projetos', 160.00),
(11, 'Consultor Especialista PD&I/Eng', 160.00);

INSERT IGNORE INTO funcionario (id_funcionario, id_cargo, nome, nivel, tipo_vinculo, status) VALUES
(1, 1, 'Paulino Rubião', 'Sênior', 'CLT', 'Ativo'),
(2, 2, 'Eduardo Sedano', 'Pleno', 'CLT', 'Ativo'),
(3, 3, 'Flavio Natal', 'Especialista', 'PJ', 'Ativo'),
(4, 4, 'Antonio Aguida', 'Sênior', 'CLT', 'Ativo'),
(5, 4, 'Roberto Souza Costa', 'Sênior', 'CLT', 'Ativo'),
(6, 5, 'Luiz Menezes', 'Pleno', 'CLT', 'Ativo'),
(7, 6, 'Evandro Lamberti', 'Sênior', 'CLT', 'Ativo'),
(8, 7, 'Clayton Sant''ana', 'Pleno', 'CLT', 'Ativo'),
(9, 7, 'Igor Alves', 'Pleno', 'CLT', 'Ativo'),
(10, 7, 'Victor Hugo Noronha', 'Pleno', 'CLT', 'Ativo'),
(11, 8, 'Lucilene Moraes', 'Sênior', 'CLT', 'Ativo'),
(12, 8, 'Gerhard Egwarth', 'Sênior', 'PJ', 'Ativo'),
(13, 9, 'Daniel Joaquim Pereira', 'Sênior', 'CLT', 'Ativo'),
(14, 10, 'Eduard Müller', 'Sênior', 'CLT', 'Ativo'),
(15, 11, 'Marco Antônio Carvalho', 'Especialista', 'PJ', 'Ativo');

-- Senhas armazenadas como hash PBKDF2/SHA256 via magal.Services.PasswordHasher
-- Admin: "admin123"
INSERT IGNORE INTO usuario (id_usuario, nome, email, senha, status, nivel) VALUES
(1, 'Admin', 'admin@aeroconcepts.com', 'V1$100000$TyFmxrsxrAKsWqUz/IgbIw==$roa4l0EWB/FXikE4tAuVnk2Vjf7ZQsOxTnq3N6fGzbg=', 'Ativo', 'Administrador'),
(2, 'user', 'user@aeroconcepts.com', 'V1$100000$9KrS/J0T3FZv8zbhebhnjw==$HZDobUb/KEkqcrkw8Gnet6GBc6dW0et31aoIvx3e9Z4=', 'Ativo', 'Operador');

INSERT IGNORE INTO cliente (id_cliente, nome, tipo, cpf_cnpj, cidade, estado, contato) VALUES
(1, 'Funcate', 'Pessoa Jurídica', '51055403876', 'São José dos Campos', 'SP', 'Contato Comercial'),
(2, 'Voith', 'Jurídica', NULL, 'São Paulo', 'SP', 'Departamento de Projetos'),
(3, 'Arauco', 'Jurídica', NULL, 'Curitiba', 'PR', 'Suprimentos'),
(4, 'EESC', 'Institucional', NULL, 'São Carlos', 'SP', 'Diretoria Técnica'),
(5, 'International Paper', 'Jurídica', NULL, 'Mogi Guaçu', 'SP', 'Engenharia'),
(6, 'Suzano', 'Jurídica', NULL, 'Salvador', 'BA', 'Gestão de Contratos'),
(7, 'Klabin', 'Jurídica', NULL, 'Telêmaco Borba', 'PR', 'Planejamento'),
(8, 'FAB', 'Governo', NULL, 'Brasília', 'DF', 'Comando da Aeronáutica'),
(9, 'IAE', 'Governo', NULL, 'São José dos Campos', 'SP', 'Diretoria IAE'),
(10, 'Birla Carbon', 'Jurídica', NULL, 'Cubatão', 'SP', 'Manutenção Industrial'),
(11, 'Rhodia', 'Jurídica', NULL, 'Paulínia', 'SP', 'Compras Técnicas'),
(12, 'Raizen', 'Jurídica', NULL, 'Piracicaba', 'SP', 'Projetos Estratégicos'),
(13, 'DCTA', 'Governo', NULL, 'São José dos Campos', 'SP', 'Secretaria de Tecnologia');

-- ==============================================================================
-- 3. CARGA DO CATÁLOGO MASTER DE CUSTOS (A MATRIZ DOS PREÇOS)
-- ==============================================================================

INSERT IGNORE INTO catalogo_custo (id_catalogo_custo, nome, categoria, valor) VALUES
(1, 'Componentes Eletrônicos de Bancada', 'EPIs/Ferramentas', 1540.00),
(2, 'Softwares de Simulação Numérica Estendida', 'Licenças de Software', 3200.00),
(3, 'Instrumentação e ferramentas de medição portátil', 'EPIs/Ferramentas', 1520.00),
(4, 'Locação de Andaimes e Estruturas Modulares', 'Aluguel/Estrutura', 15000.00),
(5, 'Manutenção Corretiva em Gerador de Campo', 'Manutenção', 12150.00),
(6, 'Passagens Aéreas e Estadia de Engenharia em Salvador', 'Transporte/Deslocamento', 12100.00),
(7, 'Controladores Lógicos Programáveis Dedicados', 'Equipamentos', 10100.00),
(8, 'Aquisição de Malhas de Deformação Alta Temperatura', 'EPIs/Ferramentas', 29200.00),
(9, 'Dutos Industriais de Exaustão Revestidos 800mm', 'Equipamentos', 15280.00),
(10, 'Locação de Analisadores de Segurança e Checklist', 'Aluguel/Estrutura', 2600.00),
(11, 'Assinatura Anual de Licenças ANSYS Aero / Hydro', 'Licenças de Software', 64200.00),
(12, 'Consumo Energético Dedicado de Gerador a Diesel Móvel', 'Energia Elétrica', 14200.00),
(13, 'Barras Metálicas Estruturais Gerdau Extra Rígidas', 'Equipamentos', 3880.00),
(14, 'Hospedagem Técnica continuada na Região de Paulínia', 'Transporte/Deslocamento', 22800.00),
(15, 'Kit de Vedação Industrial O-Ring', 'EPIs/Ferramentas', 4500.00),
(16, 'Aluguel de Guindaste Hidráulico', 'Aluguel/Estrutura', 8000.00),
(17, 'Seguro de Risco de Engenharia', 'Aluguel/Estrutura', 2500.00),
(18, 'Calibração de Sensores de Vibração Fluke', 'Manutenção', 3200.00),
(19, 'CLP Siemens S7-1500 + Módulos I/O', 'Equipamentos', 12300.00),
(20, 'Gabinete Metálico Rittal com Climatizador', 'Equipamentos', 4200.00),
(21, 'Bornes de Conexão Push-In Phoenix Contact', 'EPIs/Ferramentas', 1150.00),
(22, 'Frete Expresso de Componentes Importados', 'Transporte/Deslocamento', 850.00),
(23, 'Licença de Software ANSYS Fluent (Uso Dedicado)', 'Licenças de Software', 15000.00),
(24, 'Processamento em Cluster de Computação de Alto Performance (HPC)', 'Aluguel/Estrutura', 8500.00),
(25, 'Consumo Adicional de Energia do Cluster de Processamento', 'Energia Elétrica', 2200.00),
(26, 'Termopares de Platina Industriais Tipo S', 'EPIs/Ferramentas', 6800.00),
(27, 'Hospedagem e Diárias da Equipe Técnica (Campo)', 'Transporte/Deslocamento', 4500.00),
(28, 'Reparo Emergencial em Duto de Combustão', 'Manutenção', 1900.00),
(29, 'Exaustor Centrífugo Industrial Anti-Fagulha 50HP', 'Equipamentos', 32400.00),
(30, 'Dutos de Aço Galvanizado Revestidos 1200mm', 'Equipamentos', 14200.00),
(31, 'Estrutura Metálica de Suporte e Fixação Externa', 'Aluguel/Estrutura', 5500.00),
(32, 'Kits de EPI Rígido para Trabalho em Altura (NR-35)', 'EPIs/Ferramentas', 3800.00),
(33, 'Mapeamento Georreferenciado por Drone (Laser Scanning)', 'Equipamentos', 7500.00),
(34, 'Deslocamento Terrestre e Combustível para Coleta em Campo', 'Transporte/Deslocamento', 1200.00);

-- ==============================================================================
-- 4. PROJETOS, CUSTOS, TAREFAS E ORÇAMENTOS (IDs EXPLÍCITOS: SCRIPT PODE SER REEXECUTADO SEM DUPLICAR)
-- ==============================================================================

-- PROJETO 01: Funcate - Projeto Antigo Teste A
INSERT IGNORE INTO projeto (id_projeto, id_usuario, id_cliente, nome, tipo, status, data_criacao, data_conclusao_prevista) VALUES
(1, 1, 1, 'Projeto Antigo Teste A', 'Serviço', 'Em Aberto', '2025-01-10 00:00:00', NULL);
INSERT IGNORE INTO custo (id_custo, id_projeto, id_catalogo_custo, nome, categoria, tipo, valor, unidade, data_cadastro) VALUES
(134, 1, 1, 'Componentes Eletrônicos de Bancada', 'EPIs/Ferramentas', 'Direto', 1540.00, 'Unitário', '2026-09-30 13:04:31'),
(135, 1, 13, 'Barras Metálicas Estruturais Gerdau Extra Rígidas', 'Equipamentos', 'Direto', 3880.00, 'Unitário', '2026-09-30 13:04:31'),
(136, 1, 29, 'Exaustor Centrífugo Industrial Anti-Fagulha 50HP', 'Equipamentos', 'Direto', 32400.00, 'Unitário', '2026-09-30 13:04:31');
INSERT IGNORE INTO tarefa (id_tarefa, id_projeto, id_funcionario, descricao, horas_estimadas, horas_reais, custo_real, status) VALUES
(105, 1, 6, 'Desenho preliminar de circuitos e placas', 20.00, 0.00, 0.00, 'Concluída');
INSERT IGNORE INTO orcamento (id_orcamento, id_projeto, custo_base, percentual_impostos, valor_impostos, margem_percentual, valor_margem, valor_final, validade_dias, forma_pagamento, prazo_entrega, observacoes, data_criacao) VALUES
(1, 1, 39220.00, 0.00, 0.00, 43.60, 17099.92, 56319.92, 30, '', NULL, '', '2026-07-20 13:46:46');

-- PROJETO 02: Funcate - Projeto Antigo Teste B
INSERT IGNORE INTO projeto (id_projeto, id_usuario, id_cliente, nome, tipo, status, data_criacao, data_conclusao_prevista) VALUES
(2, 1, 1, 'Projeto Antigo Teste B', 'Produto', 'Em Aberto', '2025-02-15 00:00:00', NULL);
INSERT IGNORE INTO custo (id_custo, id_projeto, id_catalogo_custo, nome, categoria, tipo, valor, unidade, data_cadastro) VALUES
(99, 2, 2, 'Softwares de Simulação Numérica Estendida', 'Licenças de Software', 'Direto', 3200.00, 'Unitário', '2026-09-29 16:51:03'),
(100, 2, 13, 'Barras Metálicas Estruturais Gerdau Extra Rígidas', 'Equipamentos', 'Direto', 3880.00, 'Unitário', '2026-09-29 16:51:03'),
(101, 2, 25, 'Consumo Adicional de Energia do Cluster de Processamento', 'Energia Elétrica', 'Direto', 2200.00, 'Unitário', '2026-09-29 16:51:03'),
(102, 2, 29, 'Exaustor Centrífugo Industrial Anti-Fagulha 50HP', 'Equipamentos', 'Direto', 32400.00, 'Unitário', '2026-09-29 16:51:03');
INSERT IGNORE INTO tarefa (id_tarefa, id_projeto, id_funcionario, descricao, horas_estimadas, horas_reais, custo_real, status) VALUES
(83, 2, 1, 'Engenharia Reversa e Mapeamento Elétrico', 24.00, 0.00, 0.00, 'Concluída'),
(84, 2, 2, 'Supervisão técnica de bancada de testes', 6.22, 0.00, 0.00, 'Concluída'),
(85, 2, 6, 'Supervisão técnica de bancada de testes', 20.00, 0.00, 0.00, 'Pendente');
INSERT IGNORE INTO orcamento (id_orcamento, id_projeto, custo_base, percentual_impostos, valor_impostos, margem_percentual, valor_margem, valor_final, validade_dias, forma_pagamento, prazo_entrega, observacoes, data_criacao) VALUES
(2, 2, 47568.70, 0.00, 0.00, 40.95, 19479.38, 67048.08, 15, '', NULL, '', '2026-07-20 13:46:46');

-- PROJETO 03: Funcate - Projeto Antigo Teste C
INSERT IGNORE INTO projeto (id_projeto, id_usuario, id_cliente, nome, tipo, status, data_criacao, data_conclusao_prevista) VALUES
(3, 1, 1, 'Projeto Antigo Teste C', 'Produto', 'Em Aberto', '2025-03-20 00:00:00', NULL);
INSERT IGNORE INTO custo (id_custo, id_projeto, id_catalogo_custo, nome, categoria, tipo, valor, unidade, data_cadastro) VALUES
(87, 3, 3, 'Instrumentação e ferramentas de medição portátil', 'EPIs/Ferramentas', 'Direto', 1520.00, 'Unitário', '2026-09-29 16:49:10'),
(88, 3, 29, 'Exaustor Centrífugo Industrial Anti-Fagulha 50HP', 'Equipamentos', 'Direto', 32400.00, 'Unitário', '2026-09-29 16:49:10');
INSERT IGNORE INTO tarefa (id_tarefa, id_projeto, id_funcionario, descricao, horas_estimadas, horas_reais, custo_real, status) VALUES
(76, 3, 8, 'Tabulação de dados e emissão de relatório inicial', 8.00, 0.00, 0.00, 'Concluída');
INSERT IGNORE INTO orcamento (id_orcamento, id_projeto, custo_base, percentual_impostos, valor_impostos, margem_percentual, valor_margem, valor_final, validade_dias, forma_pagamento, prazo_entrega, observacoes, data_criacao) VALUES
(3, 3, 34520.00, 0.00, 0.00, 46.61, 16089.77, 50609.77, 10, '', NULL, '', '2026-07-20 13:46:46');

-- PROJETO 04: Voith - Modernização de Turbina Hidrelétrica VT-01
INSERT IGNORE INTO projeto (id_projeto, id_usuario, id_cliente, nome, tipo, status, data_criacao, data_conclusao_prevista) VALUES
(4, 1, 2, 'Modernização de Turbina Hidrelétrica VT-01', 'Serviço', 'Executando', '2026-05-22 09:00:00', NULL);
INSERT IGNORE INTO custo (id_custo, id_projeto, id_catalogo_custo, nome, categoria, tipo, valor, unidade, data_cadastro) VALUES
(123, 4, 5, 'Manutenção Corretiva em Gerador de Campo', 'Manutenção', 'Direto', 12150.00, 'Unitário', '2026-09-29 16:54:44');
INSERT IGNORE INTO tarefa (id_tarefa, id_projeto, id_funcionario, descricao, horas_estimadas, horas_reais, custo_real, status) VALUES
(98, 4, 3, 'Análise de vibração e dinâmica de fluidos preliminar', 80.00, 0.00, 0.00, 'Executando');
INSERT IGNORE INTO orcamento (id_orcamento, id_projeto, custo_base, percentual_impostos, valor_impostos, margem_percentual, valor_margem, valor_final, validade_dias, forma_pagamento, prazo_entrega, observacoes, data_criacao) VALUES
(4, 4, 31350.00, 12.00, 4702.50, 25.00, 7837.50, 43890.00, 40, '', NULL, '', '2026-07-20 13:46:46');

-- PROJETO 05: Suzano - Otimização de Linha de Celulose - Planta BA
INSERT IGNORE INTO projeto (id_projeto, id_usuario, id_cliente, nome, tipo, status, data_criacao, data_conclusao_prevista) VALUES
(5, 1, 6, 'Otimização de Linha de Celulose - Planta BA', 'Serviço', 'Aprovado', '2026-05-06 14:30:00', NULL);
INSERT IGNORE INTO custo (id_custo, id_projeto, id_catalogo_custo, nome, categoria, tipo, valor, unidade, data_cadastro) VALUES
(6, 5, 6, 'Passagens Aéreas e Estadia de Engenharia em Salvador', 'Transporte/Deslocamento', 'Direto', 12100.00, 'Unitário', '2026-07-20 13:46:46');
INSERT IGNORE INTO tarefa (id_tarefa, id_projeto, id_funcionario, descricao, horas_estimadas, horas_reais, custo_real, status) VALUES
(6, 5, 7, 'Revisão de malhas de automação da planta industrial', 60.00, 0.00, 0.00, 'Concluída'),
(7, 5, 6, 'Apoio técnico em levantamento de campo P&ID', 64.28, 0.00, 0.00, 'Concluída');
INSERT IGNORE INTO orcamento (id_orcamento, id_projeto, custo_base, percentual_impostos, valor_impostos, margem_percentual, valor_margem, valor_final, validade_dias, forma_pagamento, prazo_entrega, observacoes, data_criacao) VALUES
(5, 5, 25599.60, 12.00, 3686.34, 20.00, 5119.92, 34405.86, 45, NULL, NULL, NULL, '2026-07-20 13:46:46');

-- PROJETO 06: Klabin - Desenvolvimento de Painel de Automação Industrial
INSERT IGNORE INTO projeto (id_projeto, id_usuario, id_cliente, nome, tipo, status, data_criacao, data_conclusao_prevista) VALUES
(6, 1, 7, 'Desenvolvimento de Painel de Automação Industrial', 'Produto', 'Orçado', '2026-05-18 10:15:00', NULL);
INSERT IGNORE INTO custo (id_custo, id_projeto, id_catalogo_custo, nome, categoria, tipo, valor, unidade, data_cadastro) VALUES
(7, 6, 7, 'Controladores Lógicos Programáveis Dedicados', 'Equipamentos', 'Direto', 10100.00, 'Unitário', '2026-07-20 13:46:46');
INSERT IGNORE INTO tarefa (id_tarefa, id_projeto, id_funcionario, descricao, horas_estimadas, horas_reais, custo_real, status) VALUES
(8, 6, 2, 'Desenvolvimento e teste das lógicas em CLP', 40.00, 0.00, 0.00, 'Pendente');
INSERT IGNORE INTO orcamento (id_orcamento, id_projeto, custo_base, percentual_impostos, valor_impostos, margem_percentual, valor_margem, valor_final, validade_dias, forma_pagamento, prazo_entrega, observacoes, data_criacao) VALUES
(6, 6, 13500.00, 18.00, 3159.00, 30.00, 4050.00, 20709.00, 40, NULL, NULL, NULL, '2026-07-20 13:46:46');

-- PROJETO 07: FAB - Análise Estrutural Flaps Aeronave T-27
INSERT IGNORE INTO projeto (id_projeto, id_usuario, id_cliente, nome, tipo, status, data_criacao, data_conclusao_prevista) VALUES
(7, 1, 8, 'Análise Estrutural Flaps Aeronave T-27', 'Serviço', 'Executando', '2026-03-01 08:00:00', NULL);
INSERT IGNORE INTO custo (id_custo, id_projeto, id_catalogo_custo, nome, categoria, tipo, valor, unidade, data_cadastro) VALUES
(8, 7, 8, 'Aquisição de Malhas de Deformação Alta Temperatura', 'EPIs/Ferramentas', 'Direto', 29200.00, 'Unitário', '2026-07-20 13:46:46');
INSERT IGNORE INTO tarefa (id_tarefa, id_projeto, id_funcionario, descricao, horas_estimadas, horas_reais, custo_real, status) VALUES
(9, 7, 15, 'Modelagem matemática estrutural de fadiga aeroespacial', 180.00, 0.00, 0.00, 'Executando');
INSERT IGNORE INTO orcamento (id_orcamento, id_projeto, custo_base, percentual_impostos, valor_impostos, margem_percentual, valor_margem, valor_final, validade_dias, forma_pagamento, prazo_entrega, observacoes, data_criacao) VALUES
(7, 7, 86800.00, 0.00, 0.00, 15.00, 13020.00, 99820.00, 60, NULL, NULL, NULL, '2026-07-20 13:46:46');

-- PROJETO 08: Arauco - Sistema de Exaustão de Resíduos Térmicos
INSERT IGNORE INTO projeto (id_projeto, id_usuario, id_cliente, nome, tipo, status, data_criacao, data_conclusao_prevista) VALUES
(8, 1, 3, 'Sistema de Exaustão de Resíduos Térmicos', 'Serviço', 'Rascunho', '2026-03-12 16:45:00', NULL);
INSERT IGNORE INTO custo (id_custo, id_projeto, id_catalogo_custo, nome, categoria, tipo, valor, unidade, data_cadastro) VALUES
(133, 8, 9, 'Dutos Industriais de Exaustão Revestidos 800mm', 'Equipamentos', 'Direto', 15280.00, 'Unitário', '2026-09-30 13:04:23');
INSERT IGNORE INTO tarefa (id_tarefa, id_projeto, id_funcionario, descricao, horas_estimadas, horas_reais, custo_real, status) VALUES
(104, 8, 4, 'Cálculo de dimensionamento de exaustão e fluxo', 27.03, 0.00, 0.00, 'Pendente');
INSERT IGNORE INTO orcamento (id_orcamento, id_projeto, custo_base, percentual_impostos, valor_impostos, margem_percentual, valor_margem, valor_final, validade_dias, forma_pagamento, prazo_entrega, observacoes, data_criacao) VALUES
(8, 8, 19334.50, 18.00, 4245.86, 22.00, 4253.59, 27833.95, 15, '', NULL, '', '2026-07-20 13:46:46');

-- PROJETO 09: International Paper - Laudo Técnico de Conformidade NR-12
INSERT IGNORE INTO projeto (id_projeto, id_usuario, id_cliente, nome, tipo, status, data_criacao, data_conclusao_prevista) VALUES
(9, 1, 5, 'Laudo Técnico de Conformidade NR-12', 'Serviço', 'Concluído', '2026-05-20 11:20:00', NULL);
INSERT IGNORE INTO custo (id_custo, id_projeto, id_catalogo_custo, nome, categoria, tipo, valor, unidade, data_cadastro) VALUES
(98, 9, 10, 'Locação de Analisadores de Segurança e Checklist', 'Aluguel/Estrutura', 'Direto', 2600.00, 'Unitário', '2026-09-29 16:50:33');
INSERT IGNORE INTO tarefa (id_tarefa, id_projeto, id_funcionario, descricao, horas_estimadas, horas_reais, custo_real, status) VALUES
(81, 9, 5, 'Inspeção física in loco das conformidades da NR-12', 40.00, 0.00, 0.00, 'Concluída'),
(82, 9, 2, 'Inspeção física in loco das conformidades da NR-12', 70.00, 0.00, 0.00, 'Pendente');
INSERT IGNORE INTO orcamento (id_orcamento, id_projeto, custo_base, percentual_impostos, valor_impostos, margem_percentual, valor_margem, valor_final, validade_dias, forma_pagamento, prazo_entrega, observacoes, data_criacao) VALUES
(9, 9, 14550.00, 12.00, 2357.10, 35.00, 5092.50, 21999.60, 25, '', NULL, '', '2026-07-20 13:46:46');

-- PROJETO 11: Raizen - Dimensionamento Elétrico Destilaria Setor Norte
INSERT IGNORE INTO projeto (id_projeto, id_usuario, id_cliente, nome, tipo, status, data_criacao, data_conclusao_prevista) VALUES
(11, 1, 12, 'Dimensionamento Elétrico Destilaria Setor Norte', 'Serviço', 'Orçado', '2026-04-19 10:00:00', NULL);
INSERT IGNORE INTO custo (id_custo, id_projeto, id_catalogo_custo, nome, categoria, tipo, valor, unidade, data_cadastro) VALUES
(12, 11, 12, 'Consumo Energético Dedicado de Gerador a Diesel Móvel', 'Energia Elétrica', 'Direto', 14200.00, 'Unitário', '2026-07-20 13:46:46');
INSERT IGNORE INTO tarefa (id_tarefa, id_projeto, id_funcionario, descricao, horas_estimadas, horas_reais, custo_real, status) VALUES
(13, 11, 1, 'Estudos de seletividade elétrica e curtos-circuitos', 120.00, 0.00, 0.00, 'Pendente');
INSERT IGNORE INTO orcamento (id_orcamento, id_projeto, custo_base, percentual_impostos, valor_impostos, margem_percentual, valor_margem, valor_final, validade_dias, forma_pagamento, prazo_entrega, observacoes, data_criacao) VALUES
(11, 11, 34000.00, 12.00, 5100.00, 25.00, 8500.00, 47600.00, 30, NULL, NULL, NULL, '2026-07-20 13:46:46');

-- PROJETO 14: Voith - Modernização Real de Turbina VT-A1
INSERT IGNORE INTO projeto (id_projeto, id_usuario, id_cliente, nome, tipo, status, data_criacao, data_conclusao_prevista) VALUES
(14, 1, 2, 'Modernização Real de Turbina VT-A1', 'Serviço', 'Executando', '2026-05-10 09:00:00', NULL);
INSERT IGNORE INTO custo (id_custo, id_projeto, id_catalogo_custo, nome, categoria, tipo, valor, unidade, data_cadastro) VALUES
(124, 14, 15, 'Kit de Vedação Industrial O-Ring', 'EPIs/Ferramentas', 'Direto', 4500.00, 'Unitário', '2026-09-29 16:55:09'),
(125, 14, 17, 'Seguro de Risco de Engenharia', 'Aluguel/Estrutura', 'Indireto', 2500.00, 'Mês', '2026-09-29 16:55:09'),
(126, 14, 18, 'Calibração de Sensores de Vibração Fluke', 'Manutenção', 'Direto', 3200.00, 'Unitário', '2026-09-29 16:55:09');
INSERT IGNORE INTO tarefa (id_tarefa, id_projeto, id_funcionario, descricao, horas_estimadas, horas_reais, custo_real, status) VALUES
(99, 14, 3, 'Análise de integridade estrutural e dinâmica de fluidos', 40.00, 0.00, 0.00, 'Concluída'),
(100, 14, 1, 'Supervisão de montagem em campo e alinhamento do rotor', 50.00, 0.00, 0.00, 'Executando'),
(101, 14, 2, 'Parametrização do módulo de proteção eletrônica', 20.00, 0.00, 0.00, 'Pendente');
INSERT IGNORE INTO orcamento (id_orcamento, id_projeto, custo_base, percentual_impostos, valor_impostos, margem_percentual, valor_margem, valor_final, validade_dias, forma_pagamento, prazo_entrega, observacoes, data_criacao) VALUES
(14, 14, 29750.00, 12.00, 4284.00, 20.00, 5950.00, 39984.00, 30, '', NULL, '', '2026-07-20 13:46:46');

-- PROJETO 15: Klabin - Desenvolvimento de Painel de Automação K-Log
INSERT IGNORE INTO projeto (id_projeto, id_usuario, id_cliente, nome, tipo, status, data_criacao, data_conclusao_prevista) VALUES
(15, 1, 7, 'Desenvolvimento de Painel de Automação K-Log', 'Produto', 'Orçado', '2026-05-12 14:00:00', NULL);
INSERT IGNORE INTO custo (id_custo, id_projeto, id_catalogo_custo, nome, categoria, tipo, valor, unidade, data_cadastro) VALUES
(19, 15, 19, 'CLP Siemens S7-1500 + Módulos I/O', 'Equipamentos', 'Direto', 12300.00, 'Unitário', '2026-07-20 13:46:46'),
(20, 15, 20, 'Gabinete Metálico Rittal com Climatizador', 'Equipamentos', 'Direto', 4200.00, 'Unitário', '2026-07-20 13:46:46'),
(21, 15, 21, 'Bornes de Conexão Push-In Phoenix Contact', 'EPIs/Ferramentas', 'Direto', 1150.00, 'Unitário', '2026-07-20 13:46:46'),
(22, 15, 22, 'Frete Expresso de Componentes Importados', 'Transporte/Deslocamento', 'Direto', 850.00, 'Unitário', '2026-07-20 13:46:46');
INSERT IGNORE INTO tarefa (id_tarefa, id_projeto, id_funcionario, descricao, horas_estimadas, horas_reais, custo_real, status) VALUES
(19, 15, 2, 'Programação da lógica do CLP e telas do supervisório IHMs', 30.00, 0.00, 0.00, 'Pendente'),
(20, 15, 6, 'Montagem interna do painel e chicotes elétricos', 25.00, 0.00, 0.00, 'Pendente'),
(21, 15, 7, 'Validação de diagramas e testes de aceitação em fábrica (FAT)', 15.00, 0.00, 0.00, 'Pendente');
INSERT IGNORE INTO orcamento (id_orcamento, id_projeto, custo_base, percentual_impostos, valor_impostos, margem_percentual, valor_margem, valor_final, validade_dias, forma_pagamento, prazo_entrega, observacoes, data_criacao) VALUES
(15, 15, 25050.00, 18.00, 5861.70, 30.00, 7515.00, 38426.70, 15, NULL, NULL, NULL, '2026-07-20 13:46:46');

-- PROJETO 17: Suzano - Otimização de Caldeira de Recuperação - Unidade BA
INSERT IGNORE INTO projeto (id_projeto, id_usuario, id_cliente, nome, tipo, status, data_criacao, data_conclusao_prevista) VALUES
(17, 1, 6, 'Otimização de Caldeira de Recuperação - Unidade BA', 'Serviço', 'Rascunho', '2026-05-18 11:00:00', NULL);
INSERT IGNORE INTO custo (id_custo, id_projeto, id_catalogo_custo, nome, categoria, tipo, valor, unidade, data_cadastro) VALUES
(26, 17, 26, 'Termopares de Platina Industriais Tipo S', 'EPIs/Ferramentas', 'Direto', 6800.00, 'Unitário', '2026-07-20 13:46:46'),
(27, 17, 27, 'Hospedagem e Diárias da Equipe Técnica (Campo)', 'Transporte/Deslocamento', 'Direto', 4500.00, 'Dia', '2026-07-20 13:46:46'),
(28, 17, 28, 'Reparo Emergencial em Duto de Combustão', 'Manutenção', 'Direto', 1900.00, 'Unitário', '2026-07-20 13:46:46');
INSERT IGNORE INTO tarefa (id_tarefa, id_projeto, id_funcionario, descricao, horas_estimadas, horas_reais, custo_real, status) VALUES
(25, 17, 1, 'Mapeamento térmico por termografia infravermelha', 24.00, 0.00, 0.00, 'Pendente'),
(26, 17, 5, 'Cálculos de balanço de massa e eficiência energética da caldeira', 35.00, 0.00, 0.00, 'Pendente'),
(27, 17, 8, 'Coleta de dados em CLP e consolidação de relatórios', 20.00, 0.00, 0.00, 'Pendente');
INSERT IGNORE INTO orcamento (id_orcamento, id_projeto, custo_base, percentual_impostos, valor_impostos, margem_percentual, valor_margem, valor_final, validade_dias, forma_pagamento, prazo_entrega, observacoes, data_criacao) VALUES
(17, 17, 23910.00, 12.00, 3586.50, 25.00, 5977.50, 33474.00, 20, NULL, NULL, NULL, '2026-07-20 13:46:46');

-- PROJETO 19: Rhodia - Estudo Integrado de Viabilidade e Layout Paulínia
INSERT IGNORE INTO projeto (id_projeto, id_usuario, id_cliente, nome, tipo, status, data_criacao, data_conclusao_prevista) VALUES
(19, 1, 11, 'Estudo Integrado de Viabilidade e Layout Paulínia', 'Serviço', 'Concluído', '2026-05-22 10:00:00', NULL);
INSERT IGNORE INTO custo (id_custo, id_projeto, id_catalogo_custo, nome, categoria, tipo, valor, unidade, data_cadastro) VALUES
(33, 19, 33, 'Mapeamento Georreferenciado por Drone (Laser Scanning)', 'Equipamentos', 'Direto', 7500.00, 'Unitário', '2026-07-20 13:46:46'),
(34, 19, 34, 'Deslocamento Terrestre e Combustível para Coleta em Campo', 'Transporte/Deslocamento', 'Indireto', 1200.00, 'Unitário', '2026-07-20 13:46:46');
INSERT IGNORE INTO tarefa (id_tarefa, id_projeto, id_funcionario, descricao, horas_estimadas, horas_reais, custo_real, status) VALUES
(31, 19, 13, 'Gerenciamento de engenharia e otimização do fluxo de processos', 50.00, 0.00, 0.00, 'Concluída'),
(32, 19, 14, 'Planejamento de cronograma, CAPEX/OPEX e restrições físicas', 40.00, 0.00, 0.00, 'Concluída'),
(33, 19, 10, 'Modelagem 3D do arranjo físico de tubulações (Plot-Plan)', 60.00, 0.00, 0.00, 'Concluída');
INSERT IGNORE INTO orcamento (id_orcamento, id_projeto, custo_base, percentual_impostos, valor_impostos, margem_percentual, valor_margem, valor_final, validade_dias, forma_pagamento, prazo_entrega, observacoes, data_criacao) VALUES
(19, 19, 35550.00, 12.00, 5545.80, 30.00, 10665.00, 51760.80, 30, NULL, NULL, NULL, '2026-07-20 13:46:47');

-- PROJETO 20: EESC - Ensaio de Vibração em Bancada de Rotores
INSERT IGNORE INTO projeto (id_projeto, id_usuario, id_cliente, nome, tipo, status, data_criacao, data_conclusao_prevista) VALUES
(20, 1, 4, 'Ensaio de Vibração em Bancada de Rotores', 'Serviço', 'Concluído', '2025-04-08 09:30:00', NULL);
INSERT IGNORE INTO custo (id_custo, id_projeto, id_catalogo_custo, nome, categoria, tipo, valor, unidade, data_cadastro) VALUES
(38, 20, 18, 'Calibração de Sensores de Vibração Fluke', 'Manutenção', 'Direto', 3200.00, 'Unitário', '2026-09-29 16:46:17'),
(39, 20, 3, 'Instrumentação e ferramentas de medição portátil', 'EPIs/Ferramentas', 'Direto', 1520.00, 'Unitário', '2026-09-29 16:46:17'),
(40, 20, 34, 'Deslocamento Terrestre e Combustível para Coleta em Campo', 'Transporte/Deslocamento', 'Indireto', 1200.00, 'Unitário', '2026-09-29 16:46:17');
INSERT IGNORE INTO tarefa (id_tarefa, id_projeto, id_funcionario, descricao, horas_estimadas, horas_reais, custo_real, status) VALUES
(37, 20, 3, 'Ensaios de vibração e análise modal do rotor', 36.00, 0.00, 0.00, 'Concluída'),
(38, 20, 6, 'Aquisição e tratamento de dados de bancada', 24.00, 0.00, 0.00, 'Concluída');
INSERT IGNORE INTO orcamento (id_orcamento, id_projeto, custo_base, percentual_impostos, valor_impostos, margem_percentual, valor_margem, valor_final, validade_dias, forma_pagamento, prazo_entrega, observacoes, data_criacao) VALUES
(23, 20, 16240.00, 12.00, 2533.44, 30.00, 4872.00, 23645.44, 30, NULL, NULL, NULL, '2026-09-29 16:46:17');

-- PROJETO 21: IAE - Análise Térmica de Câmara de Combustão
INSERT IGNORE INTO projeto (id_projeto, id_usuario, id_cliente, nome, tipo, status, data_criacao, data_conclusao_prevista) VALUES
(21, 1, 9, 'Análise Térmica de Câmara de Combustão', 'Serviço', 'Concluído', '2025-05-14 14:00:00', NULL);
INSERT IGNORE INTO custo (id_custo, id_projeto, id_catalogo_custo, nome, categoria, tipo, valor, unidade, data_cadastro) VALUES
(41, 21, 2, 'Softwares de Simulação Numérica Estendida', 'Licenças de Software', 'Direto', 3200.00, 'Unitário', '2026-09-29 16:46:18'),
(42, 21, 24, 'Processamento em Cluster de Computação de Alto Performance (HPC)', 'Aluguel/Estrutura', 'Direto', 8500.00, 'Hora', '2026-09-29 16:46:18');
INSERT IGNORE INTO tarefa (id_tarefa, id_projeto, id_funcionario, descricao, horas_estimadas, horas_reais, custo_real, status) VALUES
(39, 21, 15, 'Modelagem térmica transiente da câmara', 90.00, 0.00, 0.00, 'Concluída'),
(40, 21, 11, 'Simulação CFD acoplada e validação dos resultados', 60.00, 0.00, 0.00, 'Concluída');
INSERT IGNORE INTO orcamento (id_orcamento, id_projeto, custo_base, percentual_impostos, valor_impostos, margem_percentual, valor_margem, valor_final, validade_dias, forma_pagamento, prazo_entrega, observacoes, data_criacao) VALUES
(24, 21, 51300.00, 0.00, 0.00, 22.00, 11286.00, 62586.00, 45, NULL, NULL, NULL, '2026-09-29 16:46:18');

-- PROJETO 22: Arauco - Retrofit de Painel de Comando de Caldeira
INSERT IGNORE INTO projeto (id_projeto, id_usuario, id_cliente, nome, tipo, status, data_criacao, data_conclusao_prevista) VALUES
(22, 1, 3, 'Retrofit de Painel de Comando de Caldeira', 'Produto', 'Aprovado', '2025-06-03 10:15:00', NULL);
INSERT IGNORE INTO custo (id_custo, id_projeto, id_catalogo_custo, nome, categoria, tipo, valor, unidade, data_cadastro) VALUES
(43, 22, 19, 'CLP Siemens S7-1500 + Módulos I/O', 'Equipamentos', 'Direto', 12300.00, 'Unitário', '2026-09-29 16:46:18'),
(44, 22, 20, 'Gabinete Metálico Rittal com Climatizador', 'Equipamentos', 'Direto', 4200.00, 'Unitário', '2026-09-29 16:46:18'),
(45, 22, 21, 'Bornes de Conexão Push-In Phoenix Contact', 'EPIs/Ferramentas', 'Direto', 1150.00, 'Unitário', '2026-09-29 16:46:18'),
(46, 22, 22, 'Frete Expresso de Componentes Importados', 'Transporte/Deslocamento', 'Direto', 850.00, 'Unitário', '2026-09-29 16:46:18');
INSERT IGNORE INTO tarefa (id_tarefa, id_projeto, id_funcionario, descricao, horas_estimadas, horas_reais, custo_real, status) VALUES
(41, 22, 2, 'Levantamento das instalações e diagramas existentes', 18.00, 0.00, 0.00, 'Concluída'),
(42, 22, 1, 'Projeto elétrico do novo painel de comando', 44.00, 0.00, 0.00, 'Concluída'),
(43, 22, 6, 'Montagem e cablagem do painel', 32.00, 0.00, 0.00, 'Concluída');
INSERT IGNORE INTO orcamento (id_orcamento, id_projeto, custo_base, percentual_impostos, valor_impostos, margem_percentual, valor_margem, valor_final, validade_dias, forma_pagamento, prazo_entrega, observacoes, data_criacao) VALUES
(25, 22, 29530.00, 18.00, 6910.02, 30.00, 8859.00, 45299.02, 30, NULL, NULL, NULL, '2026-09-29 16:46:18');

-- PROJETO 23: Voith - Inspeção e Reparo de Rotor de Turbina Francis
INSERT IGNORE INTO projeto (id_projeto, id_usuario, id_cliente, nome, tipo, status, data_criacao, data_conclusao_prevista) VALUES
(23, 1, 2, 'Inspeção e Reparo de Rotor de Turbina Francis', 'Serviço', 'Concluído', '2025-07-21 08:00:00', NULL);
INSERT IGNORE INTO custo (id_custo, id_projeto, id_catalogo_custo, nome, categoria, tipo, valor, unidade, data_cadastro) VALUES
(47, 23, 5, 'Manutenção Corretiva em Gerador de Campo', 'Manutenção', 'Direto', 12150.00, 'Unitário', '2026-09-29 16:46:18'),
(48, 23, 16, 'Aluguel de Guindaste Hidráulico', 'Aluguel/Estrutura', 'Direto', 8000.00, 'Dia', '2026-09-29 16:46:18'),
(49, 23, 17, 'Seguro de Risco de Engenharia', 'Aluguel/Estrutura', 'Indireto', 2500.00, 'Mês', '2026-09-29 16:46:18'),
(50, 23, 32, 'Kits de EPI Rígido para Trabalho em Altura (NR-35)', 'EPIs/Ferramentas', 'Indireto', 3800.00, 'Unitário', '2026-09-29 16:46:18');
INSERT IGNORE INTO tarefa (id_tarefa, id_projeto, id_funcionario, descricao, horas_estimadas, horas_reais, custo_real, status) VALUES
(44, 23, 3, 'Inspeção dimensional e ensaios não destrutivos do rotor', 70.00, 0.00, 0.00, 'Concluída'),
(45, 23, 4, 'Coordenação dos serviços de reparo em campo', 60.00, 0.00, 0.00, 'Concluída'),
(46, 23, 5, 'Acompanhamento de montagem e comissionamento', 48.00, 0.00, 0.00, 'Concluída');
INSERT IGNORE INTO orcamento (id_orcamento, id_projeto, custo_base, percentual_impostos, valor_impostos, margem_percentual, valor_margem, valor_final, validade_dias, forma_pagamento, prazo_entrega, observacoes, data_criacao) VALUES
(26, 23, 59450.00, 12.00, 8917.50, 25.00, 14862.50, 83230.00, 40, NULL, NULL, NULL, '2026-09-29 16:46:18');

-- PROJETO 24: Suzano - Balanceamento Dinâmico de Ventiladores de Tiragem
INSERT IGNORE INTO projeto (id_projeto, id_usuario, id_cliente, nome, tipo, status, data_criacao, data_conclusao_prevista) VALUES
(24, 1, 6, 'Balanceamento Dinâmico de Ventiladores de Tiragem', 'Serviço', 'Concluído', '2025-08-11 13:45:00', NULL);
INSERT IGNORE INTO custo (id_custo, id_projeto, id_catalogo_custo, nome, categoria, tipo, valor, unidade, data_cadastro) VALUES
(51, 24, 18, 'Calibração de Sensores de Vibração Fluke', 'Manutenção', 'Direto', 3200.00, 'Unitário', '2026-09-29 16:46:18'),
(52, 24, 27, 'Hospedagem e Diárias da Equipe Técnica (Campo)', 'Transporte/Deslocamento', 'Direto', 4500.00, 'Dia', '2026-09-29 16:46:18'),
(53, 24, 34, 'Deslocamento Terrestre e Combustível para Coleta em Campo', 'Transporte/Deslocamento', 'Indireto', 1200.00, 'Unitário', '2026-09-29 16:46:18');
INSERT IGNORE INTO tarefa (id_tarefa, id_projeto, id_funcionario, descricao, horas_estimadas, horas_reais, custo_real, status) VALUES
(47, 24, 4, 'Medição de vibração e balanceamento em campo', 40.00, 0.00, 0.00, 'Concluída'),
(48, 24, 8, 'Análise espectral e relatório técnico', 24.00, 0.00, 0.00, 'Concluída');
INSERT IGNORE INTO orcamento (id_orcamento, id_projeto, custo_base, percentual_impostos, valor_impostos, margem_percentual, valor_margem, valor_final, validade_dias, forma_pagamento, prazo_entrega, observacoes, data_criacao) VALUES
(27, 24, 16700.00, 12.00, 2565.12, 28.00, 4676.00, 23941.12, 20, NULL, NULL, NULL, '2026-09-29 16:46:18');

-- PROJETO 25: FAB - Qualificação de Sistema Aviônico de Teste
INSERT IGNORE INTO projeto (id_projeto, id_usuario, id_cliente, nome, tipo, status, data_criacao, data_conclusao_prevista) VALUES
(25, 1, 8, 'Qualificação de Sistema Aviônico de Teste', 'Produto', 'Executando', '2025-09-09 09:00:00', NULL);
INSERT IGNORE INTO custo (id_custo, id_projeto, id_catalogo_custo, nome, categoria, tipo, valor, unidade, data_cadastro) VALUES
(54, 25, 7, 'Controladores Lógicos Programáveis Dedicados', 'Equipamentos', 'Direto', 10100.00, 'Unitário', '2026-09-29 16:46:18'),
(55, 25, 19, 'CLP Siemens S7-1500 + Módulos I/O', 'Equipamentos', 'Direto', 12300.00, 'Unitário', '2026-09-29 16:46:18'),
(56, 25, 3, 'Instrumentação e ferramentas de medição portátil', 'EPIs/Ferramentas', 'Direto', 1520.00, 'Unitário', '2026-09-29 16:46:18');
INSERT IGNORE INTO tarefa (id_tarefa, id_projeto, id_funcionario, descricao, horas_estimadas, horas_reais, custo_real, status) VALUES
(49, 25, 15, 'Definição do plano de qualificação e requisitos', 80.00, 0.00, 0.00, 'Concluída'),
(50, 25, 11, 'Desenvolvimento do software de ensaio', 110.00, 0.00, 0.00, 'Executando'),
(51, 25, 9, 'Integração de hardware e testes funcionais', 70.00, 0.00, 0.00, 'Executando');
INSERT IGNORE INTO orcamento (id_orcamento, id_projeto, custo_base, percentual_impostos, valor_impostos, margem_percentual, valor_margem, valor_final, validade_dias, forma_pagamento, prazo_entrega, observacoes, data_criacao) VALUES
(28, 25, 74570.00, 0.00, 0.00, 20.00, 14914.00, 89484.00, 60, NULL, NULL, NULL, '2026-09-29 16:46:18');

-- PROJETO 26: Klabin - Automação de Linha de Corte e Embalagem
INSERT IGNORE INTO projeto (id_projeto, id_usuario, id_cliente, nome, tipo, status, data_criacao, data_conclusao_prevista) VALUES
(26, 1, 7, 'Automação de Linha de Corte e Embalagem', 'Produto', 'Executando', '2025-10-06 11:30:00', NULL);
INSERT IGNORE INTO custo (id_custo, id_projeto, id_catalogo_custo, nome, categoria, tipo, valor, unidade, data_cadastro) VALUES
(57, 26, 19, 'CLP Siemens S7-1500 + Módulos I/O', 'Equipamentos', 'Direto', 12300.00, 'Unitário', '2026-09-29 16:46:18'),
(58, 26, 20, 'Gabinete Metálico Rittal com Climatizador', 'Equipamentos', 'Direto', 4200.00, 'Unitário', '2026-09-29 16:46:18'),
(59, 26, 21, 'Bornes de Conexão Push-In Phoenix Contact', 'EPIs/Ferramentas', 'Direto', 1150.00, 'Unitário', '2026-09-29 16:46:18'),
(60, 26, 7, 'Controladores Lógicos Programáveis Dedicados', 'Equipamentos', 'Direto', 10100.00, 'Unitário', '2026-09-29 16:46:18');
INSERT IGNORE INTO tarefa (id_tarefa, id_projeto, id_funcionario, descricao, horas_estimadas, horas_reais, custo_real, status) VALUES
(52, 26, 2, 'Especificação funcional e arquitetura de automação', 30.00, 0.00, 0.00, 'Concluída'),
(53, 26, 1, 'Projeto elétrico e dimensionamento de acionamentos', 56.00, 0.00, 0.00, 'Concluída'),
(54, 26, 10, 'Programação de CLP e telas de operação', 64.00, 0.00, 0.00, 'Executando');
INSERT IGNORE INTO orcamento (id_orcamento, id_projeto, custo_base, percentual_impostos, valor_impostos, margem_percentual, valor_margem, valor_final, validade_dias, forma_pagamento, prazo_entrega, observacoes, data_criacao) VALUES
(29, 26, 44340.00, 18.00, 10375.56, 30.00, 13302.00, 68017.56, 45, NULL, NULL, NULL, '2026-09-29 16:46:18');

-- PROJETO 27: Birla Carbon - Inspeção Termográfica de Fornos de Reação
INSERT IGNORE INTO projeto (id_projeto, id_usuario, id_cliente, nome, tipo, status, data_criacao, data_conclusao_prevista) VALUES
(27, 1, 10, 'Inspeção Termográfica de Fornos de Reação', 'Serviço', 'Concluído', '2025-11-17 08:30:00', NULL);
INSERT IGNORE INTO custo (id_custo, id_projeto, id_catalogo_custo, nome, categoria, tipo, valor, unidade, data_cadastro) VALUES
(119, 27, 26, 'Termopares de Platina Industriais Tipo S', 'EPIs/Ferramentas', 'Direto', 6800.00, 'Unitário', '2026-09-29 16:52:58'),
(120, 27, 27, 'Hospedagem e Diárias da Equipe Técnica (Campo)', 'Transporte/Deslocamento', 'Direto', 4500.00, 'Dia', '2026-09-29 16:52:58'),
(121, 27, 3, 'Instrumentação e ferramentas de medição portátil', 'EPIs/Ferramentas', 'Direto', 1520.00, 'Unitário', '2026-09-29 16:52:58'),
(122, 27, 30, 'Dutos de Aço Galvanizado Revestidos 1200mm', 'Equipamentos', 'Direto', 14200.00, 'Unitário', '2026-09-29 16:52:58');
INSERT IGNORE INTO tarefa (id_tarefa, id_projeto, id_funcionario, descricao, horas_estimadas, horas_reais, custo_real, status) VALUES
(96, 27, 5, 'Inspeção termográfica em campo dos fornos', 32.00, 0.00, 0.00, 'Concluída'),
(97, 27, 6, 'Análise dos pontos críticos e emissão de laudo', 22.00, 0.00, 0.00, 'Concluída');
INSERT IGNORE INTO orcamento (id_orcamento, id_projeto, custo_base, percentual_impostos, valor_impostos, margem_percentual, valor_margem, valor_final, validade_dias, forma_pagamento, prazo_entrega, observacoes, data_criacao) VALUES
(30, 27, 33360.00, 12.00, 5004.00, 25.00, 8340.00, 46704.00, 20, '', NULL, '', '2026-09-29 16:46:18');

-- PROJETO 28: Rhodia - Adequação Elétrica de Subestação Auxiliar
INSERT IGNORE INTO projeto (id_projeto, id_usuario, id_cliente, nome, tipo, status, data_criacao, data_conclusao_prevista) VALUES
(28, 1, 11, 'Adequação Elétrica de Subestação Auxiliar', 'Serviço', 'Aprovado', '2025-12-09 15:00:00', NULL);
INSERT IGNORE INTO custo (id_custo, id_projeto, id_catalogo_custo, nome, categoria, tipo, valor, unidade, data_cadastro) VALUES
(64, 28, 10, 'Locação de Analisadores de Segurança e Checklist', 'Aluguel/Estrutura', 'Direto', 2600.00, 'Unitário', '2026-09-29 16:46:18'),
(65, 28, 21, 'Bornes de Conexão Push-In Phoenix Contact', 'EPIs/Ferramentas', 'Direto', 1150.00, 'Unitário', '2026-09-29 16:46:18'),
(66, 28, 22, 'Frete Expresso de Componentes Importados', 'Transporte/Deslocamento', 'Direto', 850.00, 'Unitário', '2026-09-29 16:46:18');
INSERT IGNORE INTO tarefa (id_tarefa, id_projeto, id_funcionario, descricao, horas_estimadas, horas_reais, custo_real, status) VALUES
(57, 28, 1, 'Estudo de curto-circuito e seletividade da proteção', 60.00, 0.00, 0.00, 'Concluída'),
(58, 28, 2, 'Supervisão da adequação dos painéis', 40.00, 0.00, 0.00, 'Pendente');
INSERT IGNORE INTO orcamento (id_orcamento, id_projeto, custo_base, percentual_impostos, valor_impostos, margem_percentual, valor_margem, valor_final, validade_dias, forma_pagamento, prazo_entrega, observacoes, data_criacao) VALUES
(31, 28, 17900.00, 12.00, 2620.56, 22.00, 3938.00, 24458.56, 30, NULL, NULL, NULL, '2026-09-29 16:46:18');

-- PROJETO 29: Raizen - Diagnóstico Energético de Moenda e Caldeira
INSERT IGNORE INTO projeto (id_projeto, id_usuario, id_cliente, nome, tipo, status, data_criacao, data_conclusao_prevista) VALUES
(29, 1, 12, 'Diagnóstico Energético de Moenda e Caldeira', 'Serviço', 'Concluído', '2026-01-20 10:00:00', NULL);
INSERT IGNORE INTO custo (id_custo, id_projeto, id_catalogo_custo, nome, categoria, tipo, valor, unidade, data_cadastro) VALUES
(67, 29, 12, 'Consumo Energético Dedicado de Gerador a Diesel Móvel', 'Energia Elétrica', 'Direto', 14200.00, 'Unitário', '2026-09-29 16:46:18'),
(68, 29, 26, 'Termopares de Platina Industriais Tipo S', 'EPIs/Ferramentas', 'Direto', 6800.00, 'Unitário', '2026-09-29 16:46:18'),
(69, 29, 34, 'Deslocamento Terrestre e Combustível para Coleta em Campo', 'Transporte/Deslocamento', 'Indireto', 1200.00, 'Unitário', '2026-09-29 16:46:18');
INSERT IGNORE INTO tarefa (id_tarefa, id_projeto, id_funcionario, descricao, horas_estimadas, horas_reais, custo_real, status) VALUES
(59, 29, 7, 'Levantamento de consumo e eficiência energética', 48.00, 0.00, 0.00, 'Concluída'),
(60, 29, 4, 'Análise de perdas térmicas e plano de melhorias', 36.00, 0.00, 0.00, 'Concluída');
INSERT IGNORE INTO orcamento (id_orcamento, id_projeto, custo_base, percentual_impostos, valor_impostos, margem_percentual, valor_margem, valor_final, validade_dias, forma_pagamento, prazo_entrega, observacoes, data_criacao) VALUES
(32, 29, 34800.00, 12.00, 5011.20, 20.00, 6960.00, 46771.20, 30, NULL, NULL, NULL, '2026-09-29 16:46:18');

-- PROJETO 30: DCTA - Simulação Aerodinâmica de Fuselagem de VANT
INSERT IGNORE INTO projeto (id_projeto, id_usuario, id_cliente, nome, tipo, status, data_criacao, data_conclusao_prevista) VALUES
(30, 1, 13, 'Simulação Aerodinâmica de Fuselagem de VANT', 'Serviço', 'Executando', '2026-02-10 09:20:00', NULL);
INSERT IGNORE INTO custo (id_custo, id_projeto, id_catalogo_custo, nome, categoria, tipo, valor, unidade, data_cadastro) VALUES
(70, 30, 23, 'Licença de Software ANSYS Fluent (Uso Dedicado)', 'Licenças de Software', 'Direto', 15000.00, 'Mês', '2026-09-29 16:46:18'),
(71, 30, 24, 'Processamento em Cluster de Computação de Alto Performance (HPC)', 'Aluguel/Estrutura', 'Direto', 8500.00, 'Hora', '2026-09-29 16:46:18'),
(72, 30, 25, 'Consumo Adicional de Energia do Cluster de Processamento', 'Energia Elétrica', 'Direto', 2200.00, 'Unitário', '2026-09-29 16:46:18');
INSERT IGNORE INTO tarefa (id_tarefa, id_projeto, id_funcionario, descricao, horas_estimadas, horas_reais, custo_real, status) VALUES
(61, 30, 15, 'Definição de casos de simulação e condições de contorno', 50.00, 0.00, 0.00, 'Concluída'),
(62, 30, 12, 'Geração de malha e simulações CFD', 100.00, 0.00, 0.00, 'Executando'),
(63, 30, 9, 'Pós-processamento e relatório de resultados', 40.00, 0.00, 0.00, 'Pendente');
INSERT IGNORE INTO orcamento (id_orcamento, id_projeto, custo_base, percentual_impostos, valor_impostos, margem_percentual, valor_margem, valor_final, validade_dias, forma_pagamento, prazo_entrega, observacoes, data_criacao) VALUES
(33, 30, 62700.00, 0.00, 0.00, 22.00, 13794.00, 76494.00, 60, NULL, NULL, NULL, '2026-09-29 16:46:18');

-- PROJETO 31: IAE - Projeto de Bancada de Ensaio de Propulsores
INSERT IGNORE INTO projeto (id_projeto, id_usuario, id_cliente, nome, tipo, status, data_criacao, data_conclusao_prevista) VALUES
(31, 1, 9, 'Projeto de Bancada de Ensaio de Propulsores', 'Produto', 'Aprovado', '2026-06-16 14:10:00', NULL);
INSERT IGNORE INTO custo (id_custo, id_projeto, id_catalogo_custo, nome, categoria, tipo, valor, unidade, data_cadastro) VALUES
(73, 31, 8, 'Aquisição de Malhas de Deformação Alta Temperatura', 'EPIs/Ferramentas', 'Direto', 29200.00, 'Unitário', '2026-09-29 16:46:18'),
(74, 31, 13, 'Barras Metálicas Estruturais Gerdau Extra Rígidas', 'Equipamentos', 'Direto', 3880.00, 'Unitário', '2026-09-29 16:46:18'),
(75, 31, 31, 'Estrutura Metálica de Suporte e Fixação Externa', 'Aluguel/Estrutura', 'Direto', 5500.00, 'Unitário', '2026-09-29 16:46:18');
INSERT IGNORE INTO tarefa (id_tarefa, id_projeto, id_funcionario, descricao, horas_estimadas, horas_reais, custo_real, status) VALUES
(64, 31, 3, 'Especificação de requisitos de ensaio e instrumentação', 44.00, 0.00, 0.00, 'Concluída'),
(65, 31, 7, 'Projeto mecânico da bancada e da estrutura de fixação', 72.00, 0.00, 0.00, 'Executando'),
(66, 31, 9, 'Desenho técnico e lista de materiais', 36.00, 0.00, 0.00, 'Pendente');
INSERT IGNORE INTO orcamento (id_orcamento, id_projeto, custo_base, percentual_impostos, valor_impostos, margem_percentual, valor_margem, valor_final, validade_dias, forma_pagamento, prazo_entrega, observacoes, data_criacao) VALUES
(34, 31, 62640.00, 0.00, 0.00, 25.00, 15660.00, 78300.00, 45, NULL, NULL, NULL, '2026-09-29 16:46:18');

-- PROJETO 32: EESC - Instrumentação de Túnel de Vento Subsônico
INSERT IGNORE INTO projeto (id_projeto, id_usuario, id_cliente, nome, tipo, status, data_criacao, data_conclusao_prevista) VALUES
(32, 1, 4, 'Instrumentação de Túnel de Vento Subsônico', 'Serviço', 'Orçado', '2026-07-14 10:40:00', NULL);
INSERT IGNORE INTO custo (id_custo, id_projeto, id_catalogo_custo, nome, categoria, tipo, valor, unidade, data_cadastro) VALUES
(127, 32, 3, 'Instrumentação e ferramentas de medição portátil', 'EPIs/Ferramentas', 'Direto', 1520.00, 'Unitário', '2026-09-30 13:04:09'),
(128, 32, 1, 'Componentes Eletrônicos de Bancada', 'EPIs/Ferramentas', 'Direto', 1540.00, 'Unitário', '2026-09-30 13:04:09'),
(129, 32, 18, 'Calibração de Sensores de Vibração Fluke', 'Manutenção', 'Direto', 3200.00, 'Unitário', '2026-09-30 13:04:09'),
(130, 32, 28, 'Reparo Emergencial em Duto de Combustão', 'Manutenção', 'Direto', 1900.00, 'Unitário', '2026-09-30 13:04:09'),
(131, 32, 20, 'Gabinete Metálico Rittal com Climatizador', 'Equipamentos', 'Direto', 4200.00, 'Unitário', '2026-09-30 13:04:09'),
(132, 32, 29, 'Exaustor Centrífugo Industrial Anti-Fagulha 50HP', 'Equipamentos', 'Direto', 32400.00, 'Unitário', '2026-09-30 13:04:09');
INSERT IGNORE INTO tarefa (id_tarefa, id_projeto, id_funcionario, descricao, horas_estimadas, horas_reais, custo_real, status) VALUES
(102, 32, 1, 'Projeto do sistema de aquisição de dados', 40.00, 0.00, 0.00, 'Pendente'),
(103, 32, 8, 'Montagem e calibração dos sensores', 30.00, 0.00, 0.00, 'Pendente');
INSERT IGNORE INTO orcamento (id_orcamento, id_projeto, custo_base, percentual_impostos, valor_impostos, margem_percentual, valor_margem, valor_final, validade_dias, forma_pagamento, prazo_entrega, observacoes, data_criacao) VALUES
(35, 32, 53610.00, 0.00, 0.00, 30.00, 16083.00, 69693.00, 30, '', NULL, '', '2026-09-29 16:46:18');

-- PROJETO 33: Funcate - Estudo de Confiabilidade de Sistema de Potência
INSERT IGNORE INTO projeto (id_projeto, id_usuario, id_cliente, nome, tipo, status, data_criacao, data_conclusao_prevista) VALUES
(33, 1, 1, 'Estudo de Confiabilidade de Sistema de Potência', 'Serviço', 'Executando', '2026-08-04 09:00:00', NULL);
INSERT IGNORE INTO custo (id_custo, id_projeto, id_catalogo_custo, nome, categoria, tipo, valor, unidade, data_cadastro) VALUES
(116, 33, 2, 'Softwares de Simulação Numérica Estendida', 'Licenças de Software', 'Direto', 3200.00, 'Unitário', '2026-09-29 16:52:40'),
(117, 33, 34, 'Deslocamento Terrestre e Combustível para Coleta em Campo', 'Transporte/Deslocamento', 'Indireto', 1200.00, 'Unitário', '2026-09-29 16:52:40'),
(118, 33, 29, 'Exaustor Centrífugo Industrial Anti-Fagulha 50HP', 'Equipamentos', 'Direto', 32400.00, 'Unitário', '2026-09-29 16:52:40');
INSERT IGNORE INTO tarefa (id_tarefa, id_projeto, id_funcionario, descricao, horas_estimadas, horas_reais, custo_real, status) VALUES
(93, 33, 1, 'Levantamento de dados de falhas e manutenção', 32.00, 0.00, 0.00, 'Concluída'),
(94, 33, 14, 'Planejamento e gestão do escopo do estudo', 24.00, 0.00, 0.00, 'Executando'),
(95, 33, 10, 'Modelagem de confiabilidade e relatório', 40.00, 0.00, 0.00, 'Pendente');
INSERT IGNORE INTO orcamento (id_orcamento, id_projeto, custo_base, percentual_impostos, valor_impostos, margem_percentual, valor_margem, valor_final, validade_dias, forma_pagamento, prazo_entrega, observacoes, data_criacao) VALUES
(36, 33, 50840.00, 12.00, 7931.04, 30.00, 15252.00, 74023.04, 60, '', NULL, '', '2026-09-29 16:46:18');

-- PROJETO 34: Voith - Modernização de Regulador de Velocidade
INSERT IGNORE INTO projeto (id_projeto, id_usuario, id_cliente, nome, tipo, status, data_criacao, data_conclusao_prevista) VALUES
(34, 1, 2, 'Modernização de Regulador de Velocidade', 'Serviço', 'Orçado', '2026-09-01 11:00:00', NULL);
INSERT IGNORE INTO custo (id_custo, id_projeto, id_catalogo_custo, nome, categoria, tipo, valor, unidade, data_cadastro) VALUES
(81, 34, 19, 'CLP Siemens S7-1500 + Módulos I/O', 'Equipamentos', 'Direto', 12300.00, 'Unitário', '2026-09-29 16:46:18'),
(82, 34, 20, 'Gabinete Metálico Rittal com Climatizador', 'Equipamentos', 'Direto', 4200.00, 'Unitário', '2026-09-29 16:46:18'),
(83, 34, 17, 'Seguro de Risco de Engenharia', 'Aluguel/Estrutura', 'Indireto', 2500.00, 'Mês', '2026-09-29 16:46:18');
INSERT IGNORE INTO tarefa (id_tarefa, id_projeto, id_funcionario, descricao, horas_estimadas, horas_reais, custo_real, status) VALUES
(72, 34, 3, 'Diagnóstico do regulador de velocidade atual', 40.00, 0.00, 0.00, 'Pendente'),
(73, 34, 2, 'Especificação e parametrização do novo regulador', 28.00, 0.00, 0.00, 'Pendente');
INSERT IGNORE INTO orcamento (id_orcamento, id_projeto, custo_base, percentual_impostos, valor_impostos, margem_percentual, valor_margem, valor_final, validade_dias, forma_pagamento, prazo_entrega, observacoes, data_criacao) VALUES
(37, 34, 30980.00, 12.00, 4647.00, 25.00, 7745.00, 43372.00, 45, NULL, NULL, NULL, '2026-09-29 16:46:18');

-- PROJETO 35: Arauco - Projeto de Sistema de Despoeiramento de Linha
INSERT IGNORE INTO projeto (id_projeto, id_usuario, id_cliente, nome, tipo, status, data_criacao, data_conclusao_prevista) VALUES
(35, 1, 3, 'Projeto de Sistema de Despoeiramento de Linha', 'Produto', 'Rascunho', '2026-09-22 16:20:00', NULL);
INSERT IGNORE INTO custo (id_custo, id_projeto, id_catalogo_custo, nome, categoria, tipo, valor, unidade, data_cadastro) VALUES
(84, 35, 29, 'Exaustor Centrífugo Industrial Anti-Fagulha 50HP', 'Equipamentos', 'Direto', 32400.00, 'Unitário', '2026-09-29 16:46:18'),
(85, 35, 30, 'Dutos de Aço Galvanizado Revestidos 1200mm', 'Equipamentos', 'Direto', 14200.00, 'Unitário', '2026-09-29 16:46:18'),
(86, 35, 32, 'Kits de EPI Rígido para Trabalho em Altura (NR-35)', 'EPIs/Ferramentas', 'Indireto', 3800.00, 'Unitário', '2026-09-29 16:46:18');
INSERT IGNORE INTO tarefa (id_tarefa, id_projeto, id_funcionario, descricao, horas_estimadas, horas_reais, custo_real, status) VALUES
(74, 35, 4, 'Dimensionamento de dutos e perda de carga', 36.00, 0.00, 0.00, 'Pendente'),
(75, 35, 6, 'Desenho técnico em CAD', 30.00, 0.00, 0.00, 'Pendente');
INSERT IGNORE INTO orcamento (id_orcamento, id_projeto, custo_base, percentual_impostos, valor_impostos, margem_percentual, valor_margem, valor_final, validade_dias, forma_pagamento, prazo_entrega, observacoes, data_criacao) VALUES
(38, 35, 57900.00, 18.00, 12714.84, 22.00, 12738.00, 83352.84, 20, NULL, NULL, NULL, '2026-09-29 16:46:18');

-- ==============================================================================
-- FIM DO SCRIPT
-- ==============================================================================
