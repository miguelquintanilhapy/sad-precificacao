using LiveCharts;
using LiveCharts.Wpf;
using magal.Data.Repositories;
using magal.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks; // ADICIONADO: Para suporte ao Task
using System.Windows;

namespace magal.ViewModels
{
    public class GraficoHistoricoViewModel : BaseModel
    {
        #region Campos Privados

        private readonly ProjetoRepository _repository;

        private string _totalFinanceiro;
        private string _totalLucro;
        private int _quantidadeProjetos;
        private string _periodoSelecionado;
        private string _categoriaMargemSelecionada;
        private bool _isLoading = true; // ADICIONADO: Estado inicial começa como carregando

        #endregion

        #region Cultura

        private static readonly CultureInfo _ptBR = new("pt-BR");

        #endregion

        #region Indicadores

        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }

        public string TotalFinanceiro
        {
            get => _totalFinanceiro;
            set { _totalFinanceiro = value; OnPropertyChanged(); }
        }

        public string TotalLucro
        {
            get => _totalLucro;
            set { _totalLucro = value; OnPropertyChanged(); }
        }

        public int QuantidadeProjetos
        {
            get => _quantidadeProjetos;
            set { _quantidadeProjetos = value; OnPropertyChanged(); }
        }

        #endregion

        #region Gráficos de Coleção

        public SeriesCollection SeriesStatus { get; set; } = new();
        public SeriesCollection SeriesTipo { get; set; } = new();

        public ChartValues<double> ValoresLucroMensal { get; set; } = new();
        public ChartValues<double> ValoresFaturamentoMensal { get; set; } = new();
        public string[] LabelsMeses { get; set; } = Array.Empty<string>();
        public Func<double, string> Formatter { get; set; }

        #endregion

        #region Gráfico Margem Média Dinâmico

        public ChartValues<double> ValoresMargemMedia { get; set; } = new();
        public string[] LabelsMargemX { get; set; } = Array.Empty<string>();

        public Func<double, string> MargemFormatter { get; set; } = value => $"{value:N1}%";

        public Func<LiveCharts.ChartPoint, string> MargemPontoFormatter { get; set; } = point => $"{point.Y:N1}%";

        /// <summary>
        /// Valor completo em reais, até os centavos (R$ 143.724,00). Usado no tooltip ao passar o mouse sobre a barra.
        /// </summary>
        public Func<LiveCharts.ChartPoint, string> ValorCompletoFormatter { get; set; } =
            point => point.Y.ToString("C2", new System.Globalization.CultureInfo("pt-BR"));

        public List<string> CategoriasMargem { get; set; } = new()
        {
            "Margem média geral",
            "Margem Média por tipo de projeto",
            "Margem média por status",
            "Margem média por cliente",
            "Margem Média por periodo"
        };

        public string CategoriaMargemSelecionada
        {
            get => _categoriaMargemSelecionada;
            set
            {
                _categoriaMargemSelecionada = value;
                OnPropertyChanged();
                ExecutarAtualizacaoMargemDinamica();
            }
        }

        private List<Projeto> _projetosFiltradosCache = new();

        #endregion

        #region Filtros e Comandos

        public List<string> Periodos { get; set; } = new()
        {
            "7 Dias", "30 Dias", "3 Meses", "6 Meses", "1 Ano", "Período completo"
        };

        public string PeriodoSelecionado
        {
            get => _periodoSelecionado;
            set
            {
                _periodoSelecionado = value;
                OnPropertyChanged();
                _ = CarregarDados();
            }
        }

        public RelayCommand AtualizarCommand { get; }

        #endregion

        #region Construtor

        public GraficoHistoricoViewModel()
        {
            _repository = new ProjetoRepository();
            AtualizarCommand = new RelayCommand(async _ => await CarregarDados());
            Formatter = value => value.ToString("C0", _ptBR);
            PeriodoSelecionado = Periodos.FirstOrDefault(p => p.Equals("30 Dias", StringComparison.OrdinalIgnoreCase)) ?? "30 Dias";
            CategoriaMargemSelecionada = CategoriasMargem.FirstOrDefault(c => c.Equals("Margem Média por periodo", StringComparison.OrdinalIgnoreCase)) ?? "Margem Média por periodo";
        }

        #endregion

        #region Métodos de Carregamento

        public async Task CarregarDados()
        {
            try
            {
                IsLoading = true;

                bool ehAdministrador = Sessao.UsuarioLogado != null && Sessao.UsuarioLogado.nivel == "Administrador";

                var projetos = (ehAdministrador
                    ? await _repository.BuscarTodos()
                    : await _repository.BuscarTodosPorUsuario(Sessao.UsuarioLogado?.id_usuario ?? 0)) ?? new List<Projeto>();
                DateTime dataLimite = DateTime.MinValue;

                switch (PeriodoSelecionado)
                {
                    case "7 Dias": dataLimite = DateTime.Now.AddDays(-7); break;
                    case "30 Dias": dataLimite = DateTime.Now.AddDays(-30); break;
                    case "3 Meses": dataLimite = DateTime.Now.AddMonths(-3); break;
                    case "6 Meses": dataLimite = DateTime.Now.AddMonths(-6); break;
                    case "1 Ano": dataLimite = DateTime.Now.AddYears(-1); break;
                    case "Período completo": dataLimite = DateTime.MinValue; break;
                }

                projetos = projetos.Where(p => p.data_criacao >= dataLimite).ToList();

                foreach (var projeto in projetos)
                {
                    if (projeto.Orcamento == null) projeto.Orcamento = new Orcamento();
                }

                _projetosFiltradosCache = projetos;
                await Task.Delay(100);

                AtualizarIndicadores(projetos);
                AtualizarGraficoMensal(projetos);
                AtualizarGraficoMargem(projetos);
                AtualizarGraficoStatus(projetos);
                AtualizarGraficoTipo(projetos);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar gráficos:\n\n{ex}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ExecutarAtualizacaoMargemDinamica()
        {
            try
            {
                AtualizarGraficoMargem(_projetosFiltradosCache);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao atualizar margem: {ex.Message}");
            }
        }

        #endregion

        #region Lógica dos Gráficos

        private void AtualizarIndicadores(List<Projeto> projetos)
        {
            decimal totalFaturamento = projetos.Sum(p => p.Orcamento?.valor_final ?? 0);
            decimal totalLucro = projetos.Sum(p => p.Orcamento?.valor_margem ?? 0);

            QuantidadeProjetos = projetos.Count;
            TotalFinanceiro = totalFaturamento.ToString("C2", _ptBR);
            TotalLucro = totalLucro.ToString("C2", _ptBR);
        }

        private void UpdateMargemValues(IEnumerable<(string Chave, double Margem)> dadosAgrupados)
        {
            var listaFinal = dadosAgrupados.ToList();
            ValoresMargemMedia = new ChartValues<double>(listaFinal.Select(x => Math.Round(x.Margem, 1)));
            LabelsMargemX = listaFinal.Select(x => x.Chave).ToArray();
        }

        private void AtualizarGraficoMargem(List<Projeto> projetos)
        {
            if (projetos == null || !projetos.Any())
            {
                ValoresMargemMedia = new ChartValues<double>();
                LabelsMargemX = Array.Empty<string>();
                OnPropertyChanged(nameof(ValoresMargemMedia));
                OnPropertyChanged(nameof(LabelsMargemX));
                return;
            }

            var projetosValidos = projetos.Where(p => p.Orcamento != null);
            IEnumerable<(string Chave, double Margem)> agrupamento;

            switch (CategoriaMargemSelecionada)
            {
                case "Margem média geral":
                    double margemGeral = projetosValidos.Any() ? (double)projetosValidos.Average(p => p.Orcamento.margem_percentual) : 0;
                    agrupamento = new List<(string, double)> { ("Geral", margemGeral) };
                    break;

                case "Margem Média por tipo de projeto":
                    agrupamento = projetosValidos
                        .GroupBy(p => p.tipo ?? "Sem Tipo")
                        .Select(g => (g.Key, Margem: (double)g.Average(p => p.Orcamento.margem_percentual)));
                    break;

                case "Margem média por status":
                    agrupamento = projetosValidos
                        .GroupBy(p => p.status ?? "Sem Status")
                        .Select(g => (g.Key, Margem: (double)g.Average(p => p.Orcamento.margem_percentual)));
                    break;

                case "Margem média por cliente":
                    agrupamento = projetosValidos
                        .GroupBy(p => p.Cliente != null ? (p.Cliente.nome ?? "Cliente " + p.id_cliente) : "Cliente " + p.id_cliente)
                        .Select(g => (g.Key, Margem: (double)g.Average(p => p.Orcamento.margem_percentual)));
                    break;

                case "Margem Média por periodo":
                    bool porDia = PeriodoSelecionado == "7 Dias" || PeriodoSelecionado == "30 Dias";
                    agrupamento = projetosValidos
                        .GroupBy(p => porDia ? p.data_criacao.ToString("dd/MM") : p.data_criacao.ToString("MMM/yy"))
                        .Select(g => (g.Key, Margem: (double)g.Average(p => p.Orcamento.margem_percentual)));
                    break;

                default:
                    agrupamento = Enumerable.Empty<(string, double)>();
                    break;
            }

            UpdateMargemValues(agrupamento);

            OnPropertyChanged(nameof(ValoresMargemMedia));
            OnPropertyChanged(nameof(LabelsMargemX));
        }

        private void AtualizarGraficoMensal(List<Projeto> projetos)
        {
            if (projetos == null || projetos.Count == 0)
            {
                ValoresLucroMensal = new ChartValues<double>();
                ValoresFaturamentoMensal = new ChartValues<double>();
                LabelsMeses = Array.Empty<string>();
                OnPropertyChanged(nameof(ValoresLucroMensal));
                OnPropertyChanged(nameof(ValoresFaturamentoMensal));
                OnPropertyChanged(nameof(LabelsMeses));
                return;
            }

            bool agruparPorDia = PeriodoSelecionado == "7 Dias" || PeriodoSelecionado == "30 Dias";

            if (agruparPorDia)
            {
                var agrupadoPorDia = projetos
                    .GroupBy(p => p.data_criacao.Date)
                    .OrderBy(g => g.Key)
                    .Select(g => new
                    {
                        Data = g.Key,
                        TotalLucro = g.Sum(p => p.Orcamento?.valor_margem ?? 0),
                        TotalFaturamento = g.Sum(p => p.Orcamento?.valor_final ?? 0)
                    }).ToList();

                ValoresLucroMensal = new ChartValues<double>(agrupadoPorDia.Select(x => (double)x.TotalLucro));
                ValoresFaturamentoMensal = new ChartValues<double>(agrupadoPorDia.Select(x => (double)x.TotalFaturamento));
                LabelsMeses = agrupadoPorDia.Select(x => x.Data.ToString("dd/MM")).ToArray();
            }
            else
            {
                var agrupadoPorMes = projetos
                    .GroupBy(p => new { Ano = p.data_criacao.Year, Mes = p.data_criacao.Month })
                    .OrderBy(g => g.Key.Ano).ThenBy(g => g.Key.Mes)
                    .Select(g => new
                    {
                        Data = new DateTime(g.Key.Ano, g.Key.Mes, 1),
                        TotalLucro = g.Sum(p => p.Orcamento?.valor_margem ?? 0),
                        TotalFaturamento = g.Sum(p => p.Orcamento?.valor_final ?? 0)
                    }).ToList();

                ValoresLucroMensal = new ChartValues<double>(agrupadoPorMes.Select(x => (double)x.TotalLucro));
                ValoresFaturamentoMensal = new ChartValues<double>(agrupadoPorMes.Select(x => (double)x.TotalFaturamento));
                LabelsMeses = agrupadoPorMes.Select(x => x.Data.ToString("MMM/yy")).ToArray();
            }

            OnPropertyChanged(nameof(ValoresLucroMensal));
            OnPropertyChanged(nameof(ValoresFaturamentoMensal));
            OnPropertyChanged(nameof(LabelsMeses));
        }

        /// <summary>
        /// Formata o rótulo exibido dentro da fatia dos gráficos de pizza (Status/Tipo)
        /// como porcentagem de participação. O valor bruto continua disponível no hover,
        /// via tooltip customizado (ContagemTooltip), que lê o ChartPoint.Y diretamente.
        /// </summary>
        private static string PontoPercentualFormatter(LiveCharts.ChartPoint ponto)
            => ponto.Participation.ToString("P1", _ptBR);

        /// <summary>
        /// Cor fixa por status, seguindo o ciclo de vida do projeto: cinza (ainda não
        /// iniciado) → âmbar (aguardando aprovação) → azul (confirmado) → laranja (em
        /// andamento) → verde (sucesso) / vermelho (interrompido). Evita que a mesma
        /// fatia mude de cor dependendo da ordem em que os status aparecem nos dados.
        /// </summary>
        private static readonly Dictionary<string, string> CoresPorStatus = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Rascunho"] = "#64748B",
            ["Em Aberto"] = "#0891B2", // status legado (projetos antigos), ainda ativo mas fora do fluxo atual
            ["Orçado"] = "#F59E0B",
            ["Aprovado"] = "#2563EB",
            ["Executando"] = "#EA580C",
            ["Concluído"] = "#16A34A",
            ["Cancelado"] = "#DC2626",
        };

        private static readonly string[] OrdemStatus =
        {
            "Rascunho", "Em Aberto", "Orçado", "Aprovado", "Executando", "Concluído", "Cancelado"
        };

        private const string CorStatusPadrao = "#334155";

        private void AtualizarGraficoStatus(List<Projeto> projetos)
        {
            var agrupado = projetos
                .GroupBy(p => p.status ?? "Sem Status")
                .Select(g => new { Nome = g.Key, Quantidade = g.Count() })
                .OrderBy(item =>
                {
                    int posicao = Array.IndexOf(OrdemStatus, item.Nome);
                    return posicao >= 0 ? posicao : int.MaxValue;
                })
                .ThenBy(item => item.Nome)
                .ToList();

            SeriesStatus = new SeriesCollection();

            foreach (var item in agrupado)
            {
                string hex = CoresPorStatus.TryGetValue(item.Nome, out var corStatus) ? corStatus : CorStatusPadrao;
                var cor = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(hex);

                SeriesStatus.Add(new PieSeries
                {
                    Title = item.Nome,
                    Values = new ChartValues<double> { item.Quantidade },
                    DataLabels = true,
                    LabelPoint = PontoPercentualFormatter,
                    Fill = new System.Windows.Media.SolidColorBrush(cor),
                    Foreground = System.Windows.Media.Brushes.White,
                    StrokeThickness = 2,
                    Stroke = System.Windows.Media.Brushes.White
                });
            }
            OnPropertyChanged(nameof(SeriesStatus));
        }

        private void UpdateSeriesTipo(List<Projeto> projetos)
        {
            var agrupado = projetos
                .GroupBy(p => p.tipo ?? "Sem Tipo")
                .Select(g => new { Nome = g.Key, Quantidade = g.Count() }).ToList();

            SeriesTipo = new SeriesCollection();
            string[] cores = { "#1E3A8A", "#0F766E", "#7C3AED", "#EA580C", "#BE123C", "#0891B2", "#4D7C0F", "#475569" };
            int index = 0;

            foreach (var item in agrupado)
            {
                var cor = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(cores[index % cores.Length]);
                SeriesTipo.Add(new PieSeries
                {
                    Title = item.Nome,
                    Values = new ChartValues<double> { item.Quantidade },
                    DataLabels = true,
                    LabelPoint = PontoPercentualFormatter,
                    Fill = new System.Windows.Media.SolidColorBrush(cor),
                    Foreground = System.Windows.Media.Brushes.White,
                    StrokeThickness = 2,
                    Stroke = System.Windows.Media.Brushes.White
                });
                index++;
            }
        }

        private void AtualizarGraficoTipo(List<Projeto> projetos)
        {
            UpdateSeriesTipo(projetos);
            OnPropertyChanged(nameof(SeriesTipo));
        }

        #endregion
    }

    /// <summary>
    /// Converte um valor em reais para o formato compacto exibido sobre as barras (R$ 950 / R$ 144 mil / R$ 1,2 mi).
    /// </summary>
    public class ValorCompactoConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double v = System.Convert.ToDouble(value ?? 0d, CultureInfo.InvariantCulture);
            var ptBR = new CultureInfo("pt-BR");

            if (Math.Abs(v) >= 1_000_000) return $"R$ {(v / 1_000_000).ToString("0.##", ptBR)} mi";
            if (Math.Abs(v) >= 1_000) return $"R$ {(v / 1_000).ToString("0.#", ptBR)} mil";
            return $"R$ {v.ToString("0", ptBR)}";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
