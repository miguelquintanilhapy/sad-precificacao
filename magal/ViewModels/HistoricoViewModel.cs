
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.ComponentModel;
using System.Windows.Data;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks; 
using Microsoft.Win32;
using magal.Models;
using magal.Data.Repositories;
using magal.Services;

namespace magal.ViewModels
{
    /// <summary>
    /// ViewModel responsável por gerenciar a tela de histórico de projetos, 
    /// controlando filtros de busca, indicadores financeiros e ações de edição/exclusão.
    /// </summary>
    public class HistoricoViewModel : BaseModel
    {
        #region Atributos e Campos Privados

        private readonly ProjetoRepository _repository;
        private Projeto _projetoSelecionado;
        private string _filtroTexto;
        private string _totalFinanceiro;
        private string _totalLucro;
        private int _quantidadeProjetos;
        private bool _isLoading = true;

        /// <summary>
        /// Cultura padrão para formatação monetária nacional.
        /// </summary>
        private static readonly CultureInfo _ptBR = new("pt-BR");

        #endregion

        #region Propriedades de Indicadores e Filtros

        /// <summary>
        /// Obtém ou define o estado de carregamento atual da ViewModel.
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Obtém ou define o valor formatado do faturamento total dos projetos visíveis.
        /// </summary>
        public string TotalFinanceiro
        {
            get => _totalFinanceiro;
            set { _totalFinanceiro = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Obtém ou define o valor formatado do lucro total (margem) dos projetos visíveis.
        /// </summary>
        public string TotalLucro
        {
            get => _totalLucro;
            set { _totalLucro = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Obtém ou define a quantidade total de projetos que atendem ao filtro atual.
        /// </summary>
        public int QuantidadeProjetos
        {
            get => _quantidadeProjetos;
            set { _quantidadeProjetos = value; OnPropertyChanged(); }
        }

        private int _quantidadeExecucao;
        /// <summary>
        /// Quantidade de projetos com status "Executando" entre os visíveis (indicador exibido ao usuário comum).
        /// </summary>
        public int QuantidadeExecucao
        {
            get => _quantidadeExecucao;
            set { _quantidadeExecucao = value; OnPropertyChanged(); }
        }

        private int _quantidadeConcluidos;
        /// <summary>
        /// Quantidade de projetos com status "Concluído" entre os visíveis (indicador exibido ao usuário comum).
        /// </summary>
        public int QuantidadeConcluidos
        {
            get => _quantidadeConcluidos;
            set { _quantidadeConcluidos = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Obtém ou define o texto de busca utilizado para filtrar os projetos na tela em tempo real.
        /// </summary>
        public string FiltroTexto
        {
            get => _filtroTexto;
            set
            {
                _filtroTexto = value;
                OnPropertyChanged();
                ProjetosView?.Refresh();
                AtualizarIndicadores();
            }
        }

        /// <summary>
        /// Obtém ou define o projeto atualmente selecionado na listagem (DataGrid).
        /// </summary>
        public Projeto ProjetoSelecionado
        {
            get => _projetoSelecionado;
            set { _projetoSelecionado = value; OnPropertyChanged(); }
        }

        #endregion

        #region Coleções e Visões de Dados

        /// <summary>
        /// Lista observável de projetos carregados do banco de dados.
        /// </summary>
        public ObservableCollection<Projeto> Projetos { get; } = new ObservableCollection<Projeto>();

        /// <summary>
        /// Visão customizada da coleção de projetos que permite a aplicação de filtros em tempo real sem perder a lista original.
        /// </summary>
        public ICollectionView ProjetosView { get; private set; }

        #endregion

        #region Comandos disparados pela View

        /// <summary>
        /// Comando para deletar um projeto do banco de dados e da listagem.
        /// </summary>
        public RelayCommand ExcluirCommand { get; }

        /// <summary>
        /// Comando para redirecionar o usuário para a tela de edição do projeto selecionado.
        /// </summary>
        public RelayCommand EditarCommand { get; }

        /// <summary>
        /// Comando para recarregar a lista do histórico a partir do banco de dados.
        /// </summary>
        public RelayCommand AtualizarCommand { get; }

        /// <summary>
        /// Comando para exportar a listagem atual filtrada de projetos para um relatório em PDF.
        /// </summary>
        public RelayCommand ExportarPdfCommand { get; }

        /// <summary>
        /// Gera e abre o PDF da proposta de um projeto direto da lista, sem precisar abrir a tela de edição.
        /// </summary>
        public RelayCommand GerarPropostaPdfCommand { get; }

        #endregion

        #region Construtores

        /// <summary>
        /// Inicializa uma nova instância da classe <see cref="HistoricoViewModel"/>, configurando os repositórios, comandos e filtros.
        /// </summary>
        public HistoricoViewModel()
        {
            _repository = new ProjetoRepository();

            // Configuração do mecanismo de filtragem do WPF
            ProjetosView = CollectionViewSource.GetDefaultView(Projetos);
            ProjetosView.Filter = FiltroDeProjetos;

            // Inicialização dos comandos mapeados para os botões da tela
            ExcluirCommand = new RelayCommand(p => ExecutarExclusao(p as Projeto));
            EditarCommand = new RelayCommand(p => ExecutarEdicao(p as Projeto));
            AtualizarCommand = new RelayCommand(async _ => await CarregarHistorico());
            ExportarPdfCommand = new RelayCommand(_ => ExecutarExportacaoPdf());
            GerarPropostaPdfCommand = new RelayCommand(p => ExecutarGeracaoPropostaPdf(p as Projeto));
             }

        #endregion

        #region Métodos Públicos

        /// <summary>
        /// Busca a lista atualizada de projetos do banco de dados e limpa os filtros da tela.
        /// </summary>
        public async Task CarregarHistorico()
        {
            try
            {
                // Garante que o estado seja verdadeiro ao iniciar 
                IsLoading = true;

                _filtroTexto = string.Empty;
                OnPropertyChanged(nameof(FiltroTexto));

                bool ehAdministrador = Sessao.UsuarioLogado != null && Sessao.UsuarioLogado.nivel == "Administrador";

                var lista = ehAdministrador
                    ? await _repository.BuscarTodos()
                    : await _repository.BuscarTodosPorUsuario(Sessao.UsuarioLogado?.id_usuario ?? 0);
                Projetos.Clear();

                foreach (var p in lista)
                {
                    if (p.Orcamento == null)
                    {
                        p.Orcamento = new Orcamento();
                    }

                    Projetos.Add(p);
                    p.OnPropertyChanged(nameof(p.DataExpiracao));
                    p.OnPropertyChanged(nameof(p.EstaVencido));
                }

                AtualizarIndicadores();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar histórico: {ex.Message}", "Aviso de Sistema", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            finally
                {
                    IsLoading = false;
                }
        }

        #endregion

        #region Métodos Auxiliares / Privados

        /// <summary>
        /// Avalia se um projeto deve ser exibido no DataGrid com base no texto inserido no campo de busca.
        /// </summary>
        private bool FiltroDeProjetos(object obj)
        {
            if (string.IsNullOrWhiteSpace(FiltroTexto)) return true;
            if (obj is not Projeto projeto) return false;

            var busca = FiltroTexto.ToLower().Trim();

            bool dataExpiracaoBate = projeto.Orcamento != null &&
                                     projeto.DataExpiracao.ToString("dd/MM/yyyy").Contains(busca);

            return (projeto.nome?.ToLower().Contains(busca) ?? false) ||
                   (projeto.status?.ToLower().Contains(busca) ?? false) ||
                   (projeto.tipo?.ToLower().Contains(busca) ?? false) ||
                   (projeto.Cliente?.nome?.ToLower().Contains(busca) ?? false) ||
                   dataExpiracaoBate;
        }

        /// <summary>
        /// Recalcula e atualiza as propriedades de resumo financeiro e quantidade com base estritamente nos projetos visíveis pós-filtro.
        /// </summary>
        private void AtualizarIndicadores()
        {
            var projetosVisiveis = ProjetosView.Cast<Projeto>().ToList();

            QuantidadeProjetos = projetosVisiveis.Count;
            QuantidadeExecucao = projetosVisiveis.Count(p => string.Equals(p.status, "Executando", StringComparison.OrdinalIgnoreCase));
            QuantidadeConcluidos = projetosVisiveis.Count(p => string.Equals(p.status, "Concluído", StringComparison.OrdinalIgnoreCase));

            //Dados financeiros (Faturamento e Lucro) são exclusivos do Administrador
            if (Sessao.UsuarioLogado != null && Sessao.UsuarioLogado.nivel == "Administrador")
            {
                decimal somaFaturamento = projetosVisiveis
                    .Where(p => p.Orcamento != null)
                    .Sum(p => p.Orcamento.valor_final);

                decimal somaLucro = projetosVisiveis
                    .Where(p => p.Orcamento != null)
                    .Sum(p => p.Orcamento.valor_margem);

                TotalFinanceiro = somaFaturamento.ToString("C2", _ptBR);
                TotalLucro = somaLucro.ToString("C2", _ptBR);
            }
            else
            {
                // Oculta e simplifica o visual para nível Operador ou menor
                TotalFinanceiro = "—";
                TotalLucro = "—";
            }
        }

        /// <summary>
        /// Pega os projetos atualmente visíveis na tela (aplicados os filtros) e gera um relatório tabular em PDF.
        /// </summary>
        private async void ExecutarExportacaoPdf()
        {
            var proyectosVisiveis = ProjetosView.Cast<Projeto>().ToList();

            if (!proyectosVisiveis.Any())
            {
                MessageBox.Show("Não há dados na tabela atual para exportar o relatório.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var sfd = new SaveFileDialog
            {
                Filter = "PDF|*.pdf",
                FileName = $"Relatorio_Projetos_{DateTime.Now:yyyyMMdd_HHmmss}"
            };

            if (sfd.ShowDialog() == true)
            {
                try
                {
                    await Task.Run(() => new PdfService().GerarRelatorioTabelaProjetos(proyectosVisiveis, sfd.FileName));
                    MessageBox.Show("Relatório de listagem exportado com sucesso!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erro ao exportar o relatório de projetos: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// Solicita a confirmação do usuário e executa a exclusão física do projeto no banco de dados e na memória.
        /// </summary>
        private async void ExecutarExclusao(Projeto projeto)
        {
            if (projeto == null) return;

            // Verifica se a sessão existe e se o nível NÃO é Administrador
            if (Sessao.UsuarioLogado == null || Sessao.UsuarioLogado.nivel != "Administrador")
            {
                MessageBox.Show(
                    "Acesso Negado!\nApenas usuários com nível 'Administrador' possuem permissão para excluir projetos.",
                    "Aero Concepts - Segurança",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return; // Interrompe a execução aqui, blindando a ação
            }

            var result = MessageBox.Show(
                $"Deseja realmente excluir o projeto '{projeto.nome}'?\nEsta ação não poderá ser desfeita.",
                "Atenção - Confirmação",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await _repository.ExcluirProjeto(projeto.id_projeto);
                    Projetos.Remove(projeto);
                    AtualizarIndicadores();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erro ao excluir: {ex.Message}", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        /// <summary>
        /// Carrega a estrutura de dados profunda do projeto selecionado e aciona a navegação da janela principal para a tela de edição.
        /// </summary>
        private async void ExecutarGeracaoPropostaPdf(Projeto projeto)
        {
            if (projeto == null) return;

            var sfd = new SaveFileDialog { Filter = "PDF|*.pdf", FileName = $"Proposta_{projeto.nome}" };
            if (sfd.ShowDialog() != true) return;

            try
            {
                IsLoading = true;

                var completo = await _repository.CarregarProjetoCompleto(projeto.id_projeto);
                if (completo == null)
                {
                    MessageBox.Show("Não foi possível carregar os detalhes deste projeto.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // O carregamento completo traz só os ids: resolve cliente e funcionários, que o PDF exibe pelo nome.
                var clientes = await new ClienteRepository().ListarTodos();
                var funcionarios = await new FuncionarioRepository().ListarTodos();

                completo.Cliente = clientes?.FirstOrDefault(c => c.id_cliente == completo.id_cliente);
                foreach (var t in completo.Tarefas)
                    t.Funcionario = funcionarios?.FirstOrDefault(f => f.id_funcionario == t.id_funcionario);

                var custos = completo.Custos.ToList();
                await Task.Run(() => new PdfService().GerarPropostaTecnica(completo, custos, sfd.FileName));

                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(sfd.FileName) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao gerar PDF: {ex.Message}", "Aviso de Erro", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async void ExecutarEdicao(Projeto projeto)
        {
            if (projeto == null) return;

            try
            {
                IsLoading = true;

                var projetoCompleto = await _repository.CarregarProjetoCompleto(projeto.id_projeto);

                if (projetoCompleto != null)
                {
                    var mainWindow = Application.Current.Windows.OfType<magal.MainWindow>().FirstOrDefault();
                    mainWindow?.IrParaEdicao(projetoCompleto);
                }
                else
                {
                    MessageBox.Show("Não foi possível carregar os detalhes deste projeto.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar edição: {ex.Message}", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            finally
            {
                IsLoading = false;
            }
        }

        #endregion
    }
}