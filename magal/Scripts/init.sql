-- ==============================================================================
-- SISTEMA: AERO CONCEPTS (SAD PRECIFICAÇÃO)
-- ESTRUTURA DO BANCO DE DADOS E CARGA INICIAL INTEGRAL (CORRIGIDA)
-- DATA: 26/05/2026
-- STATUS: 100% ALINHADO (CUSTOS VINCULADOS DIRETAMENTE AO CATÁLOGO MASTER)
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
    custo_hora DECIMAL(18, 2) NULL,
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
(1, 'Engenheiro Elétrico', 160.00), 
(2, 'Supervisor Eletroeletrônico', 130.00), 
(3, 'Engenheiro Especialista Turbomáquinas', 220.00), 
(4, 'Coordenador Técnico de Serviços', 145.00), 
(5, 'Analista de Engenharia Industrial', 95.00), 
(6, 'Coordenador de Engenharia Industrial', 155.00), 
(7, 'Analista de PD&I', 105.00), 
(8, 'Engenheiro de PD&I', 175.00), 
(9, 'Gerente de Engenharia', 250.00), 
(10, 'Gerente de Projetos', 230.00), 
(11, 'Consultor Especialista PD&I/Eng', 300.00);

INSERT IGNORE INTO funcionario (id_funcionario, id_cargo, nome, custo_hora, nivel, tipo_vinculo, status) VALUES 
(1, 1, 'Paulino Rubião', NULL, 'Sênior', 'CLT', 'Ativo'), 
(2, 2, 'Eduardo Sedano', NULL, 'Pleno', 'CLT', 'Ativo'), 
(3, 3, 'Flavio Natal', NULL, 'Especialista', 'PJ', 'Ativo'), 
(4, 4, 'Antonio Aguida', NULL, 'Sênior', 'CLT', 'Ativo'), 
(5, 4, 'Roberto Souza Costa', NULL, 'Sênior', 'CLT', 'Ativo'), 
(6, 5, 'Luiz Menezes', NULL, 'Pleno', 'CLT', 'Ativo'), 
(7, 6, 'Evandro Lamberti', NULL, 'Sênior', 'CLT', 'Ativo'), 
(8, 7, 'Clayton Sant''ana', NULL, 'Pleno', 'CLT', 'Ativo'), 
(9, 7, 'Igor Alves', NULL, 'Pleno', 'CLT', 'Ativo'), 
(10, 7, 'Victor Hugo Noronha', NULL, 'Pleno', 'CLT', 'Ativo'), 
(11, 8, 'Lucilene Moraes', NULL, 'Sênior', 'CLT', 'Ativo'), 
(12, 8, 'Gerhard Egwarth', NULL, 'Sênior', 'PJ', 'Ativo'), 
(13, 9, 'Daniel Joaquim Pereira', NULL, 'Sênior', 'CLT', 'Ativo'), 
(14, 10, 'Eduard Müller', NULL, 'Sênior', 'CLT', 'Ativo'), 
(15, 11, 'Marco Antônio Carvalho', NULL, 'Especialista', 'PJ', 'Ativo');

-- Senha do seed: "admin123" (armazenada como hash PBKDF2/SHA256 via magal.Services.PasswordHasher)
INSERT IGNORE INTO usuario (id_usuario, nome, email, senha, status, nivel) VALUES
(1, 'Admin', 'admin@aeroconcepts.com', 'V1$100000$Q+O7xoYL7XQ+/mWMXogH5Q==$y+x5ikbjvH6ZKeEaCEXrHLgFuiHoKzXc1ttMgew/gU4=', 'Ativo', 'Administrador');

INSERT IGNORE INTO cliente (id_cliente, nome, tipo, cidade, estado, contato) VALUES 
(1, 'Funcate', 'Jurídica', 'São José dos Campos', 'SP', 'Contato Comercial'), 
(2, 'Voith', 'Jurídica', 'São Paulo', 'SP', 'Departamento de Projetos'), 
(3, 'Arauco', 'Jurídica', 'Curitiba', 'PR', 'Suprimentos'), 
(4, 'EESC', 'Institucional', 'São Carlos', 'SP', 'Diretoria Técnica'), 
(5, 'International Paper', 'Jurídica', 'Mogi Guaçu', 'SP', 'Engenharia'), 
(6, 'Suzano', 'Jurídica', 'Salvador', 'BA', 'Gestão de Contratos'), 
(7, 'Klabin', 'Jurídica', 'Telêmaco Borba', 'PR', 'Planejamento'), 
(8, 'FAB', 'Governo', 'Brasília', 'DF', 'Comando da Aeronáutica'), 
(9, 'IAE', 'Governo', 'São José dos Campos', 'SP', 'Diretoria IAE'), 
(10, 'Birla Carbon', 'Jurídica', 'Cubatão', 'SP', 'Manutenção Industrial'), 
(11, 'Rhodia', 'Jurídica', 'Paulínia', 'SP', 'Compras Técnicas'), 
(12, 'Raizen', 'Jurídica', 'Piracicaba', 'SP', 'Projetos Estratégicos'), 
(13, 'DCTA', 'Governo', 'São José dos Campos', 'SP', 'Secretaria de Tecnologia');

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
-- 4. HISTÓRICO E PROJETOS VINCULADOS AO CATÁLOGO MASTER
-- ==============================================================================

-- PROJETO 01: Funcate
INSERT INTO projeto (id_projeto, nome, data_criacao, status, tipo, id_cliente, id_usuario) 
VALUES (1, 'Projeto Antigo Teste A', '2025-01-10', 'Em Aberto', 'Produto', 1, 1);

INSERT INTO custo (id_projeto, id_catalogo_custo, nome, categoria, tipo, valor, unidade) VALUES
(1, 1, 'Componentes Eletrônicos de Bancada', 'EPIs/Ferramentas', 'Direto', 1540.00, 'Unitário');

INSERT INTO tarefa (id_projeto, id_funcionario, descricao, horas_estimadas, status) VALUES
(1, 6, 'Desenho preliminar de circuitos e placas', 20.00, 'Concluída'); 

INSERT INTO orcamento (id_projeto, custo_base, percentual_impostos, valor_impostos, margem_percentual, valor_margem, valor_final, validade_dias) 
VALUES (1, 3440.00, 0.00, 0.00, 43.6047, 1500.00, 4939.84, 30);


-- PROJETO 02: Funcate
INSERT INTO projeto (id_projeto, nome, data_criacao, status, tipo, id_cliente, id_usuario) 
VALUES (2, 'Projeto Antigo Teste B', '2025-02-15', 'Em Aberto', 'Produto', 1, 1);

INSERT INTO custo (id_projeto, id_catalogo_custo, nome, categoria, tipo, valor, unidade) VALUES
(2, 2, 'Softwares de Simulação Numérica Estendida', 'Licenças de Software', 'Direto', 3200.00, 'Unitário');

INSERT INTO tarefa (id_projeto, id_funcionario, descricao, horas_estimadas, status) VALUES
(2, 1, 'Engenharia Reversa e Mapeamento Elétrico', 24.00, 'Concluída'), 
(2, 2, 'Supervisão técnica de bancada de testes', 6.22, 'Concluída');  

INSERT INTO orcamento (id_projeto, custo_base, percentual_impostos, valor_impostos, margem_percentual, valor_margem, valor_final, validade_dias) 
VALUES (2, 9768.60, 0.00, 0.00, 40.9475, 4000.00, 13768.84, 15);


-- PROJETO 03: Funcate
INSERT INTO projeto (id_projeto, nome, data_criacao, status, tipo, id_cliente, id_usuario) 
VALUES (3, 'Projeto Antigo Teste C', '2025-03-20', 'Em Aberto', 'Produto', 1, 1);

INSERT INTO custo (id_projeto, id_catalogo_custo, nome, categoria, tipo, valor, unidade) VALUES
(3, 3, 'Instrumentação e ferramentas de medição portátil', 'EPIs/Ferramentas', 'Direto', 1520.00, 'Unitário');

INSERT INTO tarefa (id_projeto, id_funcionario, descricao, horas_estimadas, status) VALUES
(3, 8, 'Tabulação de dados e emissão de relatório inicial', 8.00, 'Concluída'); 

INSERT INTO orcamento (id_projeto, custo_base, percentual_impostos, valor_impostos, margem_percentual, valor_margem, valor_final, validade_dias) 
VALUES (3, 2360.00, 0.00, 0.00, 46.6102, 1100.00, 3460.00, 10);


-- PROJETO 04: Voith
INSERT INTO projeto (id_projeto, nome, data_criacao, status, tipo, id_cliente, id_usuario) 
VALUES (4, 'Modernização de Turbina Hidrelétrica VT-01', '2026-05-22 09:00:00', 'Executando', 'Serviço', 2, 1);

INSERT INTO custo (id_projeto, id_catalogo_custo, nome, categoria, tipo, valor, unidade) VALUES
(4, 4, 'Locação de Andaimes e Estruturas Modulares', 'Aluguel/Estrutura', 'Direto', 15000.00, 'Mês'),
(4, 5, 'Manutenção Corretiva em Gerador de Campo', 'Manutenção', 'Direto', 12150.00, 'Unitário');

INSERT INTO tarefa (id_projeto, id_funcionario, descricao, horas_estimadas, status) VALUES
(4, 3, 'Análise de vibração e dinâmica de fluidos preliminar', 80.00, 'Executando'); 

INSERT INTO orcamento (id_projeto, custo_base, percentual_impostos, valor_impostos, margem_percentual, valor_margem, valor_final, validade_dias) 
VALUES (4, 62350.00, 12.00, 7482.00, 25.00, 15587.50, 87290.00, 40);


-- PROJETO 05: Suzano
INSERT INTO projeto (id_projeto, nome, data_criacao, status, tipo, id_cliente, id_usuario) 
VALUES (5, 'Otimização de Linha de Celulose - Planta BA', '2026-05-06 14:30:00', 'Aprovado', 'Serviço', 6, 1);

INSERT INTO custo (id_projeto, id_catalogo_custo, nome, categoria, tipo, valor, unidade) VALUES
(5, 6, 'Passagens Aéreas e Estadia de Engenharia em Salvador', 'Transporte/Deslocamento', 'Direto', 12100.00, 'Unitário');

INSERT INTO tarefa (id_projeto, id_funcionario, descricao, horas_estimadas, status) VALUES
(5, 7, 'Revisão de malhas de automação da planta industrial', 60.00, 'Concluída'), 
(5, 6, 'Apoio técnico em levantamento de campo P&ID', 64.28, 'Concluída'); 

INSERT INTO orcamento (id_projeto, custo_base, percentual_impostos, valor_impostos, margem_percentual, valor_margem, valor_final, validade_dias) 
VALUES (5, 32156.60, 12.00, 3858.79, 20.00, 6431.32, 43218.47, 45);


-- PROJETO 06: Klabin
INSERT INTO projeto (id_projeto, nome, data_criacao, status, tipo, id_cliente, id_usuario) 
VALUES (6, 'Desenvolvimento de Painel de Automação Industrial', '2026-05-18 10:15:00', 'Orçado', 'Produto', 7, 1);

INSERT INTO custo (id_projeto, id_catalogo_custo, nome, categoria, tipo, valor, unidade) VALUES
(6, 7, 'Controladores Lógicos Programáveis Dedicados', 'Equipamentos', 'Direto', 10100.00, 'Unitário');

INSERT INTO tarefa (id_projeto, id_funcionario, descricao, horas_estimadas, status) VALUES
(6, 2, 'Desenvolvimento e teste das lógicas em CLP', 40.00, 'Pendente'); 

INSERT INTO orcamento (id_projeto, custo_base, percentual_impostos, valor_impostos, margem_percentual, valor_margem, valor_final, validade_dias) 
VALUES (6, 15300.00, 18.00, 2754.00, 30.00, 4590.00, 23470.20, 40);


-- PROJETO 07: FAB
INSERT INTO projeto (id_projeto, nome, data_criacao, status, tipo, id_cliente, id_usuario) 
VALUES (7, 'Análise Estrutural Flaps Aeronave T-27', '2026-03-01 08:00:00', 'Executando', 'Serviço', 8, 1);

INSERT INTO custo (id_projeto, id_catalogo_custo, nome, categoria, tipo, valor, unidade) VALUES
(7, 8, 'Aquisição de Malhas de Deformação Alta Temperatura', 'EPIs/Ferramentas', 'Direto', 29200.00, 'Unitário');

INSERT INTO tarefa (id_projeto, id_funcionario, descricao, horas_estimadas, status) VALUES
(7, 15, 'Modelagem matemática estrutural de fadiga aeroespacial', 180.00, 'Executando'); 

INSERT INTO orcamento (id_projeto, custo_base, percentual_impostos, valor_impostos, margem_percentual, valor_margem, valor_final, validade_dias) 
VALUES (7, 137200.00, 0.00, 0.00, 15.00, 20580.00, 157780.00, 60);


-- PROJETO 08: Arauco
INSERT INTO projeto (id_projeto, nome, data_criacao, status, tipo, id_cliente, id_usuario) 
VALUES (8, 'Sistema de Exaustão de Resíduos Térmicos', '2026-03-12 16:45:00', 'Rascunho', 'Produto', 3, 1);

INSERT INTO custo (id_projeto, id_catalogo_custo, nome, categoria, tipo, valor, unidade) VALUES
(8, 9, 'Dutos Industriais de Exaustão Revestidos 800mm', 'Equipamentos', 'Direto', 15280.00, 'Unitário');

INSERT INTO tarefa (id_projeto, id_funcionario, descricao, horas_estimadas, status) VALUES
(8, 4, 'Cálculo de dimensionamento de exaustão e fluxo', 27.03, 'Pendente'); 

INSERT INTO orcamento (id_projeto, custo_base, percentual_impostos, valor_impostos, margem_percentual, valor_margem, valor_final, validade_dias) 
VALUES (8, 21159.03, 18.00, 3808.63, 22.00, 4654.99, 30460.53, 15);


-- PROJETO 09: International Paper
INSERT INTO projeto (id_projeto, nome, data_criacao, status, tipo, id_cliente, id_usuario) 
VALUES (9, 'Laudo Técnico de Conformidade NR-12', '2026-05-20 11:20:00', 'Concluído', 'Serviço', 5, 1);

INSERT INTO custo (id_projeto, id_catalogo_custo, nome, categoria, tipo, valor, unidade) VALUES
(9, 10, 'Locação de Analisadores de Segurança e Checklist', 'Aluguel/Estrutura', 'Direto', 2600.00, 'Unitário');

INSERT INTO tarefa (id_projeto, id_funcionario, descricao, horas_estimadas, status) VALUES
(9, 5, 'Inspeção física in loco das conformidades da NR-12', 40.00, 'Concluída'); 

INSERT INTO orcamento (id_projeto, custo_base, percentual_impostos, valor_impostos, margem_percentual, valor_margem, valor_final, validade_dias) 
VALUES (9, 11300.00, 12.00, 1356.00, 35.00, 3955.00, 17085.60, 25);


-- PROJETO 10: DCTA
INSERT INTO projeto (id_projeto, nome, data_criacao, status, tipo, id_cliente, id_usuario) 
VALUES (10, 'Consultoria em Dinâmica de Fluidos Computacional (CFD)', '2026-05-05 13:00:00', 'Aprovado', 'Serviço', 13, 1);

INSERT INTO custo (id_projeto, id_catalogo_custo, nome, categoria, tipo, valor, unidade) VALUES
(10, 11, 'Assinatura Anual de Licenças ANSYS Aero / Hydro', 'Licenças de Software', 'Direto', 64200.00, 'Unitário');

INSERT INTO tarefa (id_projeto, id_funcionario, descricao, horas_estimadas, status) VALUES
(10, 15, 'Geração de malhas computacionais complexas e refinadas', 180.00, 'Pendente'); 

INSERT INTO orcamento (id_projeto, custo_base, percentual_impostos, valor_impostos, margem_percentual, valor_margem, valor_final, validade_dias) 
VALUES (10, 172200.00, 0.00, 0.00, 18.00, 30996.00, 203196.00, 45);


-- PROJETO 11: Raizen
INSERT INTO projeto (id_projeto, nome, data_criacao, status, tipo, id_cliente, id_usuario) 
VALUES (11, 'Dimensionamento Elétrico Destilaria Setor Norte', '2026-04-19 10:00:00', 'Orçado', 'Serviço', 12, 1);

INSERT INTO custo (id_projeto, id_catalogo_custo, nome, categoria, tipo, valor, unidade) VALUES
(11, 12, 'Consumo Energético Dedicado de Gerador a Diesel Móvel', 'Energia Elétrica', 'Direto', 14200.00, 'Unitário');

INSERT INTO tarefa (id_projeto, id_funcionario, descricao, horas_estimadas, status) VALUES
(11, 1, 'Estudos de seletividade elétrica e curtos-circuitos', 120.00, 'Pendente'); 

INSERT INTO orcamento (id_projeto, custo_base, percentual_impostos, valor_impostos, margem_percentual, valor_margem, valor_final, validade_dias) 
VALUES (11, 43000.00, 12.00, 5160.00, 25.00, 10750.00, 60200.00, 30);


-- PROJETO 12: Birla Carbon
INSERT INTO projeto (id_projeto, nome, data_criacao, status, tipo, id_cliente, id_usuario) 
VALUES (12, 'Desenvolvimento de Dispositivo de Içamento Mecânico', '2026-05-22 15:30:00', 'Rascunho', 'Produto', 10, 1);

INSERT INTO custo (id_projeto, id_catalogo_custo, nome, categoria, tipo, valor, unidade) VALUES
(12, 13, 'Barras Metálicas Estruturais Gerdau Extra Rígidas', 'Equipamentos', 'Direto', 3880.00, 'Unitário');

INSERT INTO tarefa (id_projeto, id_funcionario, descricao, horas_estimadas, status) VALUES
(12, 4, 'Cálculo analítico de elementos de máquina e olhais', 27.00, 'Pendente'); 

INSERT INTO orcamento (id_projeto, custo_base, percentual_impostos, valor_impostos, margem_percentual, valor_margem, valor_final, validade_dias) 
VALUES (12, 9752.50, 18.00, 1755.45, 28.00, 2730.70, 14730.18, 25);


-- PROJETO 13: Rhodia
INSERT INTO projeto (id_projeto, nome, data_criacao, status, tipo, id_cliente, id_usuario) 
VALUES (13, 'Estudo de Viabilidade Técnico - Planta Paulínia', '2026-05-15 08:45:00', 'Orçado', 'Serviço', 11, 1);

INSERT INTO custo (id_projeto, id_catalogo_custo, nome, categoria, tipo, valor, unidade) VALUES
(13, 14, 'Hospedagem Técnica continuada na Região de Paulínia', 'Transporte/Deslocamento', 'Direto', 22800.00, 'Unitário');

INSERT INTO tarefa (id_projeto, id_funcionario, descricao, horas_estimadas, status) VALUES
(13, 14, 'Estudo detalhado de CAPEX/OPEX e restrições de layout', 120.00, 'Pendente'); 

INSERT INTO orcamento (id_projeto, custo_base, percentual_impostos, valor_impostos, margem_percentual, valor_margem, valor_final, validade_dias) 
VALUES (13, 64200.00, 12.00, 7704.00, 22.00, 14124.00, 87722.88, 30);


-- ==============================================================================
-- 5. MASSA DE DADOS REALISTA (PROJETOS 14 AO 19 VINCULADOS)
-- ==============================================================================

-- PROJETO 14: Voith
INSERT INTO projeto (id_projeto, id_usuario, id_cliente, nome, tipo, status, data_criacao) 
VALUES (14, 1, 2, 'Modernização Real de Turbina VT-A1', 'Serviço', 'Executando', '2026-05-10 09:00:00');

INSERT INTO custo (id_projeto, id_catalogo_custo, nome, categoria, tipo, valor, unidade) VALUES
(14, 15, 'Kit de Vedação Industrial O-Ring', 'EPIs/Ferramentas', 'Direto', 4500.00, 'Unitário'),
(14, 16, 'Aluguel de Guindaste Hidráulico', 'Aluguel/Estrutura', 'Direto', 8000.00, 'Dia'),
(14, 17, 'Seguro de Risco de Engenharia', 'Aluguel/Estrutura', 'Indireto', 2500.00, 'Mês'),
(14, 18, 'Calibração de Sensores de Vibração Fluke', 'Manutenção', 'Direto', 3200.00, 'Unitário');

INSERT INTO tarefa (id_projeto, id_funcionario, descricao, horas_estimadas, status) VALUES
(14, 3, 'Análise de integridade estrutural e dinâmica de fluidos', 40.00, 'Concluída'), 
(14, 1, 'Supervisão de montagem em campo e alinhamento do rotor', 50.00, 'Executando'), 
(14, 2, 'Parametrização do módulo de proteção eletrônica', 20.00, 'Pendente');

INSERT INTO orcamento (id_projeto, custo_base, percentual_impostos, valor_impostos, margem_percentual, valor_margem, valor_final, validade_dias) 
VALUES (14, 50400.00, 12.00, 6048.00, 25.00, 12600.00, 70560.00, 30);


-- PROJETO 15: Klabin
INSERT INTO projeto (id_projeto, id_usuario, id_cliente, nome, tipo, status, data_criacao) 
VALUES (15, 1, 7, 'Desenvolvimento de Painel de Automação K-Log', 'Produto', 'Orçado', '2026-05-12 14:00:00');

INSERT INTO custo (id_projeto, id_catalogo_custo, nome, categoria, tipo, valor, unidade) VALUES
(15, 19, 'CLP Siemens S7-1500 + Módulos I/O', 'Equipamentos', 'Direto', 12300.00, 'Unitário'),
(15, 20, 'Gabinete Metálico Rittal com Climatizador', 'Equipamentos', 'Direto', 4200.00, 'Unitário'),
(15, 21, 'Bornes de Conexão Push-In Phoenix Contact', 'EPIs/Ferramentas', 'Direto', 1150.00, 'Unitário'),
(15, 22, 'Frete Expresso de Componentes Importados', 'Transporte/Deslocamento', 'Direto', 850.00, 'Unitário');

INSERT INTO tarefa (id_projeto, id_funcionario, descricao, horas_estimadas, status) VALUES
(15, 2, 'Programação da lógica do CLP e telas do supervisório IHMs', 30.00, 'Pendente'), 
(15, 6, 'Montagem interna do painel e chicotes elétricos', 25.00, 'Pendente'), 
(15, 7, 'Validação de diagramas e testes de aceitação em fábrica (FAT)', 15.00, 'Pendente');

INSERT INTO orcamento (id_projeto, custo_base, percentual_impostos, valor_impostos, margem_percentual, valor_margem, valor_final, validade_dias) 
VALUES (15, 28262.50, 18.00, 5087.25, 30.00, 8478.75, 43354.68, 15);


-- PROJETO 16: DCTA
INSERT INTO projeto (id_projeto, id_usuario, id_cliente, nome, tipo, status, data_criacao) 
VALUES (16, 1, 13, 'Análise Aerodinâmica Avançada CFD - Suborbital', 'Serviço', 'Aprovado', '2026-05-15 08:00:00');

INSERT INTO custo (id_projeto, id_catalogo_custo, nome, categoria, tipo, valor, unidade) VALUES
(16, 23, 'Licença de Software ANSYS Fluent (Uso Dedicado)', 'Licenças de Software', 'Direto', 15000.00, 'Mês'),
(16, 24, 'Processamento em Cluster de Computação de Alto Performance (HPC)', 'Aluguel/Estrutura', 'Direto', 8500.00, 'Hora'),
(16, 25, 'Consumo Adicional de Energia do Cluster de Processamento', 'Energia Elétrica', 'Direto', 2200.00, 'Unitário');

INSERT INTO tarefa (id_projeto, id_funcionario, descricao, horas_estimadas, status) VALUES
(16, 15, 'Modelagem matemática e validação de malha computacional', 60.00, 'Pendente'), 
(16, 11, 'Processamento de cenários e relatórios de arrasto', 40.00, 'Pendente'), 
(16, 12, 'Revisão por par e validação cruzada dos coeficientes balísticos', 12.00, 'Pendente');

INSERT INTO orcamento (id_projeto, custo_base, percentual_impostos, valor_impostos, margem_percentual, valor_margem, valor_final, validade_dias) 
VALUES (16, 75350.00, 0.00, 0.00, 20.00, 15070.00, 90420.00, 45);


-- PROJETO 17: Suzano
INSERT INTO projeto (id_projeto, id_usuario, id_cliente, nome, tipo, status, data_criacao) 
VALUES (17, 1, 6, 'Otimização de Caldeira de Recuperação - Unidade BA', 'Serviço', 'Rascunho', '2026-05-18 11:00:00');

INSERT INTO custo (id_projeto, id_catalogo_custo, nome, categoria, tipo, valor, unidade) VALUES
(17, 26, 'Termopares de Platina Industriais Tipo S', 'EPIs/Ferramentas', 'Direto', 6800.00, 'Unitário'),
(17, 27, 'Hospedagem e Diárias da Equipe Técnica (Campo)', 'Transporte/Deslocamento', 'Direto', 4500.00, 'Dia'),
(17, 28, 'Reparo Emergencial em Duto de Combustão', 'Manutenção', 'Direto', 1900.00, 'Unitário');

INSERT INTO tarefa (id_projeto, id_funcionario, descricao, horas_estimadas, status) VALUES
(17, 1, 'Mapeamento térmico por termografia infravermelha', 24.00, 'Pendente'), 
(17, 5, 'Cálculos de balanço de massa e eficiência energética da caldeira', 35.00, 'Pendente'), 
(17, 8, 'Coleta de dados em CLP e consolidação de relatórios', 20.00, 'Pendente');

INSERT INTO orcamento (id_projeto, custo_base, percentual_impostos, valor_impostos, margem_percentual, valor_margem, valor_final, validade_dias) 
VALUES (17, 28672.50, 12.00, 3440.70, 25.00, 7168.13, 40141.50, 20);


-- PROJETO 18: Arauco
INSERT INTO projeto (id_projeto, id_usuario, id_cliente, nome, tipo, status, data_criacao) 
VALUES (18, 1, 3, 'Sistema de Exaustão de Resíduos Térmicos AR-2', 'Produto', 'Orçado', '2026-05-20 09:15:00');

INSERT INTO custo (id_projeto, id_catalogo_custo, nome, categoria, tipo, valor, unidade) VALUES
(18, 29, 'Exaustor Centrífugo Industrial Anti-Fagulha 50HP', 'Equipamentos', 'Direto', 32400.00, 'Unitário'),
(18, 30, 'Dutos de Aço Galvanizado Revestidos 1200mm', 'Equipamentos', 'Direto', 14200.00, 'Unitário'),
(18, 31, 'Estrutura Metálica de Suporte e Fixação Externa', 'Aluguel/Estrutura', 'Direto', 5500.00, 'Unitário'),
(18, 32, 'Kits de EPI Rígido para Trabalho em Altura (NR-35)', 'EPIs/Ferramentas', 'Indireto', 3800.00, 'Unitário');

INSERT INTO tarefa (id_projeto, id_funcionario, descricao, horas_estimadas, status) VALUES
(18, 4, 'Dimensionamento mecânico da rede de dutos e perda de carga', 32.00, 'Pendente'), 
(18, 6, 'Desenho Técnico detalhado em CAD/BIM estrutural', 40.00, 'Pendente'), 
(18, 9, 'Análise de dispersão e particulados na atmosfera', 15.00, 'Pendente');

INSERT INTO orcamento (id_projeto, custo_base, percentual_impostos, valor_impostos, margem_percentual, valor_margem, valor_final, validade_dias) 
VALUES (18, 70635.00, 18.00, 12714.30, 22.00, 15539.70, 98231.11, 15);


-- PROJETO 19: Rhodia
INSERT INTO projeto (id_projeto, id_usuario, id_cliente, nome, tipo, status, data_criacao) 
VALUES (19, 1, 11, 'Estudo Integrado de Viabilidade e Layout Paulínia', 'Serviço', 'Concluído', '2026-05-22 10:00:00');

INSERT INTO custo (id_projeto, id_catalogo_custo, nome, categoria, tipo, valor, unidade) VALUES
(19, 33, 'Mapeamento Georreferenciado por Drone (Laser Scanning)', 'Equipamentos', 'Direto', 7500.00, 'Unitário'),
(19, 34, 'Deslocamento Terrestre e Combustível para Coleta em Campo', 'Transporte/Deslocamento', 'Indireto', 1200.00, 'Unitário');

INSERT INTO tarefa (id_projeto, id_funcionario, descricao, horas_estimadas, status) VALUES
(19, 13, 'Gerenciamento de engenharia e otimização do fluxo de processos', 50.00, 'Concluída'), 
(19, 14, 'Planejamento de cronograma, CAPEX/OPEX e restrições físicas', 40.00, 'Concluída'), 
(19, 10, 'Modelagem 3D do arranjo físico de tubulações (Plot-Plan)', 60.00, 'Concluída');

INSERT INTO orcamento (id_projeto, custo_base, percentual_impostos, valor_impostos, margem_percentual, valor_margem, valor_final, validade_dias) 
VALUES (19, 47550.00, 12.00, 5706.00, 30.00, 14265.00, 69232.80, 30);

-- ==============================================================================
-- FIM DO SCRIPT
