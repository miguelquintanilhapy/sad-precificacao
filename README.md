# ✈️ SAD 

Sistema desktop para apoio à elaboração de orçamentos, gestão de projetos e análise financeira, desenvolvido como Trabalho de Conclusão de Curso (TCC) do Curso Técnico em Informática.

O objetivo é centralizar o gerenciamento de projetos, clientes, funcionários e custos, permitindo orçamentos mais precisos e o acompanhamento de indicadores financeiros por meio de dashboards e relatórios.

## Índice

- [Funcionalidades](#-funcionalidades)
- [Tecnologias](#️-tecnologias)
- [Arquitetura](#️-arquitetura)
- [Como executar](#-como-executar)
- [Status do projeto](#-status-do-projeto)
- [Equipe](#-equipe)

## 🚀 Funcionalidades

| Módulo | Recursos |
|---|---|
| 🔐 Login | Autenticação, controle de sessão, cadastro de usuários |
| 📋 Orçamentos | Criação de projetos, seleção de clientes/funcionários, associação de custos, margem de lucro, impostos, geração de proposta |
| 📚 Histórico | Consulta, filtros de pesquisa, edição de projetos existentes |
| 👥 Clientes | Cadastro, edição, exclusão, pesquisa, exportação em PDF |
| 👨‍💼 Funcionários | Cadastro, edição, exclusão, pesquisa, exportação em PDF |
| 🏢 Cargos | Cadastro, edição, exclusão |
| 💰 Custos | Catálogo, categorias, valores de referência, pesquisa, exportação em PDF |
| 📊 Dashboard | Indicadores (projetos, faturamento, lucro estimado), gráficos (lucro x faturamento, projetos por status/tipo), filtros por período (7d, 30d, 6m, 1a, histórico completo) |
| 📄 Relatórios | Exportação em PDF de clientes, funcionários, custos e projetos |

## ⚙️ Tecnologias

- **UI:** WPF (.NET) + XAML, padrão MVVM
- **Linguagem/Runtime:** C# / .NET 10
- **Banco de dados:** MySQL (`MySql.Data`)
- **Gráficos:** LiveCharts.Wpf
- **Relatórios:** QuestPDF

## 🏗️ Arquitetura

```text
magal/
├── Models
├── Views          # XAML + code-behind
├── ViewModels
├── Data
│   └── Repositories
├── Services        # Hash de senha, throttle de login, geração de PDF...
└── Scripts         # init.sql (schema + carga inicial)
```

## 🖥️ Como executar

### Pré-requisitos

- Windows com [.NET 10 SDK](https://dotnet.microsoft.com/download)
- MySQL Server acessível (local ou remoto)

### 1. Banco de dados

Execute `magal/Scripts/init.sql` em uma instância MySQL para criar o schema `sad_precificacao` e a carga inicial.

### 2. Configuração da connection string

O arquivo `magal/appsettings.json` fica versionado com a connection string vazia por padrão. Crie um `magal/appsettings.local.json` (ignorado pelo git) com suas credenciais reais — ele sobrescreve o `appsettings.json` em tempo de execução:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=SEU_HOST;Port=3306;Database=sad_precificacao;Uid=SEU_USUARIO;Pwd=SUA_SENHA;SslMode=Disabled;AllowPublicKeyRetrieval=True;"
  }
}
```

> ⚠️ Nunca commite credenciais reais — use sempre o `appsettings.local.json` para isso.

### 3. Build e execução

```bash
dotnet build magal/magal.csproj
dotnet run --project magal/magal.csproj
```

## 📌 Status do projeto

Em desenvolvimento. Já implementado:

- ✅ Autenticação com hash de senha e throttle de login
- ✅ Gestão de clientes, funcionários, cargos e custos
- ✅ Orçamentos e histórico de projetos
- ✅ Dashboard financeiro e relatórios em PDF

Próximas melhorias:

- Recomendação inteligente de recursos
- Indicadores avançados e dashboard executivo
- Relatórios analíticos avançados

## 👨‍💻 Equipe

Projeto desenvolvido como Trabalho de Conclusão de Curso (TCC).

- João Guilherme Pereira Mendes
- Vinícius Brisolla de Vasconcelos
- Miguel Quintanilha Gomes de Sales
