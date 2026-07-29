using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Microsoft.Win32;
using magal.Models;
using magal.Data.Repositories;
using magal.Services;
using System.Windows.Input;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace magal.ViewModels
{
    /// <summary>
    /// ViewModel responsável por gerenciar a elaboração e edição de orçamentos de projetos,
    /// controlando custos diretos, alocação de equipe (tarefas), margens, impostos e exportação em PDF.
    /// </summary>
    public class OrcamentoViewModel : BaseModel
    {
        #region Atributos e Campos Privados;.

        private Projeto _projetoAtual;
        private bool _processando = false;
        private bool _isUpdating = false;

        // Variáveis para controle de descarte e verificação de concorrência/mudanças
        private Projeto _projetoOriginal;
        private List<decimal> _custosValoresOriginais;
        private decimal _valorFinalOriginal;
        private decimal _totalHorasOriginal;

        // Lista global que armazena TODOS os custos cadastrados vindos do catálogo
        private List<CatalogoCusto> _todosOsCustosCadastrados = new List<CatalogoCusto>();
        #endregion

        #region Propriedades e Filtros

        /// <summary>
        /// Obtém ou define o projeto que está sendo orçado ou editado atualmente na tela.
        /// </summary>
        public Projeto ProjetoAtual
        {
            get => _projetoAtual;
            set
            {
                _projetoAtual = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DataExpiracaoFormatada));
                AssinarEventosOrcamento();
            }
        }

        /// <summary>
        /// Obtém a string contendo a data limite de validade da proposta comercial calculada por extenso.
        /// </summary>
        public string DataExpiracaoFormatada
        {
            get
            {
                if (ProjetoAtual?.Orcamento == null) return "-";

                DateTime dataBase = ProjetoAtual.data_criacao;

                if (dataBase == DateTime.MinValue)
                    dataBase = DateTime.Now;

                return dataBase.AddDays(ProjetoAtual.Orcamento.validade_dias).ToString("dd/MM/yyyy");
            }
        }

        /// <summary>
        /// Obtém o estado de permissão do botão de execução (Bloqueia reentrância caso o fluxo esteja processando).
        /// </summary>
        public bool BotaoAtivo => !_processando;

        /// <summary>
        /// Determina o comportamento visual do botão de descarte (Exibido apenas em modo de edição).
        /// </summary>
        public Visibility VisibilidadeBotaoDescartar =>
            (_projetoOriginal != null) ? Visibility.Visible : Visibility.Collapsed;

        #endregion

        #region Coleções Estáticas e Listas Auxiliares

        /// <summary>
        /// Fonte de dados com todos os clientes elegíveis para vinculação ao projeto.
        /// </summary>
        public ObservableCollection<Cliente> Clientes { get; } = new ObservableCollection<Cliente>();

        /// <summary>
        /// Fonte de dados contendo os funcionários ativos para vinculação nas tarefas operacionais.
        /// </summary>
        public ObservableCollection<Funcionario> Funcionarios { get; } = new ObservableCollection<Funcionario>();

        /// <summary>
        /// Lista de custos adicionais e despesas diretas associadas ao projeto atual.
        /// Adaptada para CustoItemViewModel para gerenciar os ComboBoxes dependentes por linha.
        /// </summary>
        public ObservableCollection<CustoItemViewModel> CustosExtras { get; } = new ObservableCollection<CustoItemViewModel>();

        /// <summary>
        /// Categorias fixas de despesas de infraestrutura e projetos.
        /// </summary>
        public List<string> CategoriasCustos { get; } = new List<string> { "Equipamentos", "Licenças de Software", "Energia Elétrica", "Transporte/Deslocamento", "Manutenção", "Aluguel/Estrutura", "EPIs/Ferramentas" };

        /// <summary>
        /// Lista de controle de fluxo de estados físicos do projeto.
        /// </summary>
        public List<string> OpcoesStatus { get; } = new List<string> { "Rascunho", "Orçado", "Aprovado", "Executando", "Concluído", "Cancelado" };

        /// <summary>
        /// Opções de enquadramento técnico do projeto.
        /// </summary>
        public List<string> OpcoesTipo { get; } = new List<string> { "Serviço", "Produto", "Consultoria", "P&D" };

        #endregion

        #region Comandos disparados pela View

        public RelayCommand AdicionarTarefaCommand { get; }
        public RelayCommand DeletarTarefaCommand { get; }
        public RelayCommand AdicionarCustoCommand { get; }
        public RelayCommand DeletarCustoCommand { get; }
        public RelayCommand GerarPdfCommand { get; }
        public RelayCommand DescartarCommand { get; }

        #endregion

        #region Construtores

        private TaskCompletionSource<bool> _dadosIniciaisCarregados = new TaskCompletionSource<bool>();


        /// <summary>
        /// Inicializa uma nova instância da classe <see cref="OrcamentoViewModel"/>, mapeando as ações e gerando um novo template limpo.
        /// </summary>
        public OrcamentoViewModel()
        {
            AdicionarTarefaCommand = new RelayCommand(_ => AdicionarTarefa());
            DeletarTarefaCommand = new RelayCommand(param => DeletarTarefa(param as Tarefa));
            AdicionarCustoCommand = new RelayCommand(_ => AdicionarCustoExtra());
            DeletarCustoCommand = new RelayCommand(param => DeletarCustoExtra(param as CustoItemViewModel));
            GerarPdfCommand = new RelayCommand(_ => ExecutarFluxoFinal());
            DescartarCommand = new RelayCommand(_ => ExecutarDescarte());

            CarregarDadosIniciais();
            NovoProjeto();
        }

        #endregion

        #region Métodos Públicos (Controle de Estado Interno)

        /// <summary>
        /// Transpõe e injeta uma instância de projeto vinda do banco de dados para a tela de edição, gerando backups de descarte.
        /// </summary>
        public async void CarregarProjetoParaEdicao(Projeto projetoDoBanco)
        {
            if (projetoDoBanco == null) return;

            _isUpdating = true;
            try
            {
                IsLoading = true;

                if (!_dadosIniciaisCarregados.Task.IsCompleted)
                {
                    await _dadosIniciaisCarregados.Task;
                }

                this.ProjetoAtual = projetoDoBanco;

                _projetoOriginal = new Projeto
                {
                    nome = projetoDoBanco.nome,
                    id_cliente = projetoDoBanco.id_cliente,
                    status = projetoDoBanco.status,
                    tipo = projetoDoBanco.tipo,
                    data_criacao = projetoDoBanco.data_criacao,
                    Orcamento = new Orcamento
                    {
                        margem_percentual = projetoDoBanco.Orcamento.margem_percentual,
                        percentual_impostos = projetoDoBanco.Orcamento.percentual_impostos,
                        validade_dias = projetoDoBanco.Orcamento.validade_dias,
                        forma_pagamento = projetoDoBanco.Orcamento.forma_pagamento,
                        prazo_entrega = projetoDoBanco.Orcamento.prazo_entrega,
                        observacoes = projetoDoBanco.Orcamento.observacoes
                    },
                    Tarefas = new ObservableCollection<Tarefa>(projetoDoBanco.Tarefas.ToList())
                };

                _custosValoresOriginais = projetoDoBanco.Custos?.Select(c => c.valor).ToList() ?? new List<decimal>();
                _valorFinalOriginal = projetoDoBanco.Orcamento?.valor_final ?? 0;
                _totalHorasOriginal = projetoDoBanco.Tarefas?.Sum(t => t.horas_estimadas) ?? 0;

                OnPropertyChanged(nameof(VisibilidadeBotaoDescartar));
                AssinarEventosOrcamento();

                if (this.ProjetoAtual.id_cliente > 0)
                    this.ProjetoAtual.Cliente = Clientes.FirstOrDefault(c => c.id_cliente == projetoDoBanco.id_cliente);

                this.CustosExtras.Clear();
                if (projetoDoBanco.Custos != null)
                {
                    foreach (var c in projetoDoBanco.Custos)
                    {
                        var itemViewModel = new CustoItemViewModel(c, _todosOsCustosCadastrados);
                        itemViewModel.PropertyChanged += (s, e) => {
                            if (e.PropertyName == nameof(CustoItemViewModel.valor)) AtualizarFinanceiro();
                        };
                        this.CustosExtras.Add(itemViewModel);
                    }

                    await System.Windows.Threading.Dispatcher.CurrentDispatcher.BeginInvoke(
                        System.Windows.Threading.DispatcherPriority.Loaded,
                        new Action(() =>
                        {
                            for (int i = 0; i < projetoDoBanco.Custos.Count; i++)
                            {
                                if (i < this.CustosExtras.Count)
                                {
                                    this.CustosExtras[i].id_catalogo_custo = 0;
                                    this.CustosExtras[i].id_catalogo_custo = projetoDoBanco.Custos.ElementAt(i).id_catalogo_custo;
                                }
                            }
                        })
                    );
                }

                foreach (var t in this.ProjetoAtual.Tarefas)
                {
                    t.Funcionario = Funcionarios.FirstOrDefault(f => f.id_funcionario == t.id_funcionario);
                    t.PropertyChanged += (s, e) => {
                        if (e.PropertyName == nameof(Tarefa.Funcionario) || e.PropertyName == nameof(Tarefa.horas_estimadas))
                            AtualizarFinanceiro();
                    };
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao carregar edição: " + ex.Message, "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            finally
            {
                _isUpdating = false;
                AtualizarFinanceiro();
                OnPropertyChanged(nameof(ProjetoAtual));
                IsLoading = false;
                await System.Threading.Tasks.Task.Delay(50);

                if (this.CustosExtras != null)
                {
                    foreach (var linhaCusto in this.CustosExtras)
                    {
                        var itemReal = linhaCusto.ItensFiltrados?.FirstOrDefault(x => x.id_catalogo_custo == linhaCusto.id_catalogo_custo);

                        if (itemReal != null)
                        {
                                            var backup = itemReal;
                            linhaCusto.ItemSelecionado = null;
                            linhaCusto.ItemSelecionado = backup;
                        }
                    }
                }
            }
        }
        #endregion

        #region Métodos Auxiliares / Privados

        private bool TemAlteracoes()
        {
            if (_projetoOriginal == null) return !string.IsNullOrWhiteSpace(ProjetoAtual.nome) || ProjetoAtual.Tarefas.Count > 0;

            bool basicoAlterado = ProjetoAtual.nome != _projetoOriginal.nome ||
                                  ProjetoAtual.id_cliente != _projetoOriginal.id_cliente ||
                                  ProjetoAtual.status != _projetoOriginal.status ||
                                  ProjetoAtual.tipo != _projetoOriginal.tipo ||
                                  ProjetoAtual.Orcamento.margem_percentual != _projetoOriginal.Orcamento.margem_percentual ||
                                  ProjetoAtual.Orcamento.percentual_impostos != _projetoOriginal.Orcamento.percentual_impostos ||
                                  ProjetoAtual.Orcamento.validade_dias != _projetoOriginal.Orcamento.validade_dias ||
                                  ProjetoAtual.Orcamento.forma_pagamento != _projetoOriginal.Orcamento.forma_pagamento ||
                                  ProjetoAtual.Orcamento.prazo_entrega != _projetoOriginal.Orcamento.prazo_entrega ||
                                  ProjetoAtual.Orcamento.observacoes != _projetoOriginal.Orcamento.observacoes;

            if (basicoAlterado) return true;

            if (ProjetoAtual.Tarefas.Count != _projetoOriginal.Tarefas.Count) return true;
            if (CustosExtras.Count != (_custosValoresOriginais?.Count ?? 0)) return true;

            decimal totalHorasAtual = ProjetoAtual.Tarefas?.Sum(t => t.horas_estimadas) ?? 0;
            if (totalHorasAtual != _totalHorasOriginal) return true;

            if (ProjetoAtual.Orcamento.valor_final != _valorFinalOriginal) return true;

            return false;
        }

        private void ExecutarDescarte()
        {
            var result = MessageBox.Show("Deseja descartar todas as alterações e voltar ao histórico?", "Atenção",
                MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                var mainWindow = Application.Current.Windows.OfType<magal.MainWindow>().FirstOrDefault();
                mainWindow?.AbrirHistorico();
            }
        }

        private void ExecutarFluxoFinal()
        {
            if (_processando) return;

            if (!TemAlteracoes())
            {
                MessageBox.Show("Nenhuma alteração foi detectada no projeto.", "Informação", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (string.IsNullOrWhiteSpace(ProjetoAtual.nome))
            {
                MessageBox.Show("O campo 'Nome do Projeto' deve ser preenchido.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (ProjetoAtual.Cliente == null)
            {
                MessageBox.Show("Selecione um cliente antes de finalizar.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

    
            var activeWindow = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);
            var dialog = new magal.Views.FinalizarPropostaDialog(this.ProjetoAtual.Orcamento);
            dialog.Owner = activeWindow;

            if (dialog.ShowDialog() != true) return;

            var confirm = MessageBox.Show("Deseja salvar as alterações deste projeto?", "Confirmar Salvamento", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                _processando = true;
                OnPropertyChanged(nameof(BotaoAtivo));

                if (SalvarNoBancoSilencioso())
                {
                    var respostaPdf = MessageBox.Show(
                        "Alterações gravadas no banco de dados com sucesso!\n\nDeseja gerar o relatório em PDF desta proposta?",
                        "Gerar PDF Opcional",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (respostaPdf == MessageBoxResult.Yes)
                    {
                        GerarRelatorioPdf();
                    }

                    MessageBox.Show("Projeto salvo com sucesso!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);

                    var mainWindow = Application.Current.Windows.OfType<magal.MainWindow>().FirstOrDefault();
                    mainWindow?.AbrirHistorico();
                }
            }
            finally
            {
                _processando = false;
                OnPropertyChanged(nameof(BotaoAtivo));
            }
        }

        private void NovoProjeto()
        {
            _isUpdating = true;
            var p = new Projeto
            {
                Orcamento = new Orcamento
                {
                    margem_percentual = 20,
                    percentual_impostos = 15,
                    validade_dias = 15,
                    forma_pagamento = "PIX", 
                    prazo_entrega = null,
                    observacoes = ""
                },
                Tarefas = new ObservableCollection<Tarefa>(),
                id_usuario = Sessao.UsuarioLogado?.id_usuario ?? 0,
                nome = "",
                status = "Rascunho",
                tipo = "Serviço",
                data_criacao = DateTime.Now
            };

            ProjetoAtual = p;
            _projetoOriginal = null;
            CustosExtras.Clear();

            AdicionarTarefa();
            AdicionarCustoExtra();

            _isUpdating = false;
            AtualizarFinanceiro();
        }

        private void AssinarEventosOrcamento()
        {
            if (ProjetoAtual?.Orcamento != null)
            {
                ProjetoAtual.Orcamento.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(Orcamento.margem_percentual) ||
                        e.PropertyName == nameof(Orcamento.percentual_impostos))
                    {
                        AtualizarFinanceiro();
                    }

                    if (e.PropertyName == nameof(Orcamento.validade_dias))
                    {
                        OnPropertyChanged(nameof(DataExpiracaoFormatada));
                    }
                };
            }
        }

        private async void CarregarDadosIniciais()
        {
            try
            {
                IsLoading = true;

                var listaClientes = await new ClienteRepository().ListarTodos();
                var listaFuncionarios = await new FuncionarioRepository().ListarTodos();

                _todosOsCustosCadastrados = (await new CatalogoCustoRepository().ListarTodos()) ?? new List<CatalogoCusto>();

                Clientes.Clear();
                foreach (var c in listaClientes) Clientes.Add(c);

                Funcionarios.Clear();
                foreach (var f in listaFuncionarios) Funcionarios.Add(f);

                if (ProjetoAtual != null)
                {
                    if (ProjetoAtual.id_cliente > 0)
                    {
                        ProjetoAtual.Cliente = Clientes.FirstOrDefault(c => c.id_cliente == ProjetoAtual.id_cliente);
                    }
                    else if (_projetoOriginal == null && Clientes.Count > 0)
                    {
                        ProjetoAtual.Cliente = Clientes[0];
                        ProjetoAtual.id_cliente = Clientes[0].id_cliente;
                    }

                    if (ProjetoAtual.Tarefas != null)
                    {
                        foreach (var t in ProjetoAtual.Tarefas)
                        {
                            if (t.id_funcionario > 0)
                            {
                                t.Funcionario = Funcionarios.FirstOrDefault(f => f.id_funcionario == t.id_funcionario);
                            }
                        }
                    }
                }

                if (CustosExtras != null && CustosExtras.Count > 0)
                {
                    foreach (var linhaCusto in CustosExtras)
                    {
                        linhaCusto.categoria = linhaCusto.categoria;
                    }
                }

                if (_projetoOriginal == null)
                {
                    NovoProjeto();
                }
                else
                {
                    OnPropertyChanged(nameof(ProjetoAtual));
                    AtualizarFinanceiro();
                }

                _dadosIniciaisCarregados.TrySetResult(true);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao carregar dados iniciais: " + ex.Message, "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                _dadosIniciaisCarregados.TrySetResult(false);
            }
            finally
            {
                             if (_projetoOriginal == null)
                {
                    IsLoading = false;
                }
            }
        }

        private void AdicionarTarefa()
        {
            var novaTarefa = new Tarefa
            {
                descricao = "",
                horas_estimadas = 0,
                Funcionario = null,          
                id_funcionario = 0
            };

            novaTarefa.PropertyChanged += (s, e) => {
                if (e.PropertyName == nameof(Tarefa.Funcionario))
                {
                    novaTarefa.id_funcionario = novaTarefa.Funcionario?.id_funcionario ?? 0;
                    AtualizarFinanceiro();
                }
                else if (e.PropertyName == nameof(Tarefa.horas_estimadas))
                {
                    AtualizarFinanceiro();
                }
            };

            ProjetoAtual.Tarefas.Add(novaTarefa);
            AtualizarFinanceiro();
        }
        private void DeletarTarefa(Tarefa tarefa)
        {
            if (tarefa != null && ProjetoAtual.Tarefas.Contains(tarefa))
            {
                string identificador = string.IsNullOrWhiteSpace(tarefa.descricao)
                    ? "esta tarefa"
                    : $"a tarefa '{tarefa.descricao}'";

                var result = MessageBox.Show($"Deseja remover {identificador}?", "Atenção",
                    MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    ProjetoAtual.Tarefas.Remove(tarefa);
                    AtualizarFinanceiro();
                }
            }
        }

        private void AdicionarCustoExtra()
        {
        
            var novoCusto = new CustoItemViewModel(_todosOsCustosCadastrados ?? new List<CatalogoCusto>())
            {
                categoria = "Equipamentos",
                tipo = "Direto",
                nome = "",
                valor = 0
            };

            novoCusto.PropertyChanged += (s, e) => {
                if (e.PropertyName == nameof(CustoItemViewModel.valor))
                    AtualizarFinanceiro();
            };
            CustosExtras.Add(novoCusto);
            AtualizarFinanceiro();
        }

        private void DeletarCustoExtra(CustoItemViewModel custo)
        {
            if (custo != null && CustosExtras.Contains(custo))
            {
                string identificador = string.IsNullOrWhiteSpace(custo.nome)
                    ? "este item"
                    : $"o custo '{custo.nome}'";

                var result = MessageBox.Show($"Deseja remover {identificador}?", "Atenção",
                    MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    CustosExtras.Remove(custo);
                    AtualizarFinanceiro();
                }
            }
        }

        private void AtualizarFinanceiro()
        {
            if (ProjetoAtual?.Orcamento == null || _isUpdating) return;

            try
            {
                var listaCustosBase = CustosExtras.Select(item => new Custo
                {
                    id_custo = item.id_custo,
                    id_projeto = item.id_projeto,
                    id_catalogo_custo = item.id_catalogo_custo,
                    nome = item.nome,
                    valor = item.valor,
                    categoria = item.categoria,
                    tipo = item.tipo,
                    unidade = item.unidade
                }).ToList();

                ProjetoAtual.Orcamento.CalcularTotal(
                    ProjetoAtual.Tarefas.ToList(),
                    listaCustosBase
                );

                OnPropertyChanged(nameof(ProjetoAtual));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Erro no cálculo: " + ex.Message);
            }
        }

        private bool SalvarNoBancoSilencioso()
        {
            try
            {
                if (ProjetoAtual.Cliente != null)
                {
                    ProjetoAtual.id_cliente = ProjetoAtual.Cliente.id_cliente;
                }

                var repo = new ProjetoRepository();

                var listaCustosBase = CustosExtras.Select(item => new Custo
                {
                    id_custo = item.id_custo,
                    id_projeto = item.id_projeto,
                    id_catalogo_custo = item.id_catalogo_custo,
                    nome = item.nome,
                    valor = item.valor,
                    categoria = item.categoria,
                    tipo = item.tipo,
                    unidade = item.unidade
                }).ToList();

                repo.SalvarProjetoCompleto(ProjetoAtual, listaCustosBase);

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao salvar dados no banco: " + ex.Message, "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
        }
        private bool GerarRelatorioPdf()
        {
            var sfd = new SaveFileDialog { Filter = "PDF|*.pdf", FileName = $"Proposta_{ProjetoAtual.nome}" };

            if (sfd.ShowDialog() == true)
            {
                try
                {
                    var listaCustosBase = CustosExtras.Select(item => new Custo
                    {
                        id_custo = item.id_custo,
                        id_projeto = item.id_projeto,
                        id_catalogo_custo = item.id_catalogo_custo,
                        nome = item.nome,
                        valor = item.valor,
                        categoria = item.categoria,
                        tipo = item.tipo,
                        unidade = item.unidade
                    }).ToList();

                    new PdfService().GerarPropostaTecnica(ProjetoAtual, listaCustosBase, sfd.FileName);
                    return true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Erro ao gerar PDF: " + ex.Message, "Aviso de Erro", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            return false;
        }

        #endregion

        public bool IsLoading
        {
            get => _processando;
            set
            {
                _processando = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(BotaoAtivo)); // Mantém o botão sincronizado também
            }
        }
    }

    #region Wrapper/ViewModel auxiliar para as Linhas de Custos Extras

    /// <summary>
    /// Classe de suporte (Wrapper) mapeada diretamente para o DataTemplate do ItemsControl.
    /// Monitora as alterações de categoria de forma isolada por linha e atualiza os filtros do ComboBox.
    /// </summary>
    public class CustoItemViewModel : Custo, INotifyPropertyChanged
    {
        private readonly List<CatalogoCusto> _listaMestraCustos;
        private ObservableCollection<CatalogoCusto> _itensFiltrados = new ObservableCollection<CatalogoCusto>();
        private CatalogoCusto _itemSelecionado;
        private bool _isSuppressingFilter = false; 

        public new string categoria
        {
            get => base.categoria;
            set
            {
                if (base.categoria != value)
                {
                    base.categoria = value;
                    OnRowPropertyChanged();
                    if (!_isSuppressingFilter)
                    {
                        FiltrarItensPorCategoria();
                    }
                }
            }
        }

        public new int id_catalogo_custo
        {
            get => base.id_catalogo_custo;
            set
            {
                if (base.id_catalogo_custo != value)
                {
                    base.id_catalogo_custo = value;
                    OnRowPropertyChanged(nameof(id_catalogo_custo));

                    if (!_isSuppressingFilter)
                    {
                        var itemNoCatalogo = ItensFiltrados?.FirstOrDefault(x => x.id_catalogo_custo == value);
                        if (itemNoCatalogo != null && _itemSelecionado != itemNoCatalogo)
                        {
                            this.nome = itemNoCatalogo.nome;
                            this.valor = itemNoCatalogo.valor;
                            _itemSelecionado = itemNoCatalogo;
                            OnRowPropertyChanged(nameof(nome));
                            OnRowPropertyChanged(nameof(valor));
                            OnRowPropertyChanged(nameof(ItemSelecionado));
                        }
                    }
                }
            }
        }

        public new decimal valor
        {
            get => base.valor;
            set
            {
                if (base.valor != value)
                {
                    base.valor = value;
                    OnRowPropertyChanged();
                }
            }
        }

        public ObservableCollection<CatalogoCusto> ItensFiltrados
        {
            get => _itensFiltrados;
            set
            {
                _itensFiltrados = value;
                OnRowPropertyChanged();
            }
        }

        public CatalogoCusto ItemSelecionado
        {
            get => _itemSelecionado;
            set
            {
                if (_itemSelecionado != value)
                {
                    _itemSelecionado = value;

                    if (_itemSelecionado != null)
                    {
                        this.id_catalogo_custo = _itemSelecionado.id_catalogo_custo;
                        this.nome = _itemSelecionado.nome;
                        this.valor = _itemSelecionado.valor;
                    }
                    else
                    {
                        this.id_catalogo_custo = 0;
                    }

                    OnRowPropertyChanged();
                    OnRowPropertyChanged(nameof(id_catalogo_custo));
                    OnRowPropertyChanged(nameof(nome));
                    OnRowPropertyChanged(nameof(valor));
                }
            }
        }

        public CustoItemViewModel(List<CatalogoCusto> listaMestra)
        {
            _listaMestraCustos = listaMestra ?? new List<CatalogoCusto>();
            FiltrarItensPorCategoria();
        }

        // Construtor para Edição (Carregamento do Banco)
        public CustoItemViewModel(Custo custoExistente, List<CatalogoCusto> listaMestra)
        {
            _listaMestraCustos = listaMestra ?? new List<CatalogoCusto>();

            _isSuppressingFilter = true;
            this.id_custo = custoExistente.id_custo;
            this.id_projeto = custoExistente.id_projeto;
            this.nome = custoExistente.nome?.Trim();
            this.valor = custoExistente.valor;
            this.categoria = custoExistente.categoria?.Trim(); 
            this.tipo = custoExistente.tipo;
            this.unidade = custoExistente.unidade;
            base.id_catalogo_custo = custoExistente.id_catalogo_custo;

            _isSuppressingFilter = false;
            FiltrarItensPorCategoria();

            _itemSelecionado = _itensFiltrados.FirstOrDefault(x => x.id_catalogo_custo == base.id_catalogo_custo)
                              ?? _itensFiltrados.FirstOrDefault(x => string.Equals(x.nome?.Trim(), this.nome, StringComparison.OrdinalIgnoreCase));

            OnRowPropertyChanged(nameof(id_catalogo_custo));
            OnRowPropertyChanged(nameof(ItemSelecionado));
        }

        private void FiltrarItensPorCategoria()
        {
            if (ItensFiltrados == null) ItensFiltrados = new ObservableCollection<CatalogoCusto>();

            string categoriaBusca = this.categoria?.Trim() ?? string.Empty;
            List<CatalogoCusto> filtrados;

            if (string.IsNullOrEmpty(categoriaBusca))
            {
                filtrados = _listaMestraCustos ?? new List<CatalogoCusto>();
            }
            else
            {
                filtrados = _listaMestraCustos?
                    .Where(c => string.Equals(c.categoria?.Trim(), categoriaBusca, StringComparison.OrdinalIgnoreCase) ||
                               (c.categoria != null && c.categoria.Trim().IndexOf(categoriaBusca, StringComparison.OrdinalIgnoreCase) >= 0))
                    .ToList() ?? new List<CatalogoCusto>();

                if (filtrados.Count == 0)
                {
                    filtrados = _listaMestraCustos ?? new List<CatalogoCusto>();
                }
            }

            // O SEGREDO: Se a lista atual já for identica à filtrada, NÃO damos Clear().
            // Isso evita que o WPF desmaque o ComboBox do nada.
            var idsAtuais = ItensFiltrados.Select(x => x.id_catalogo_custo).ToList();
            var idsNovos = filtrados.Select(x => x.id_catalogo_custo).ToList();

            if (!idsAtuais.SequenceEqual(idsNovos))
            {
                ItensFiltrados.Clear();
                foreach (var item in filtrados)
                {
                    ItensFiltrados.Add(item);
                }
                OnRowPropertyChanged(nameof(ItensFiltrados));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnRowPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    #endregion
    #region Extensão Auxiliar para Silenciar Notificações na Inicialização

    public static class BindableExtensions
    {
        public static IDisposable DisablePropertyChange(this object obj)
        {
            return new DummyDisposable();
        }
        private class DummyDisposable : IDisposable
        {
            public void Dispose() { /* Não faz nada, apenas evita o NullReference */ }
        }
    }

    #endregion
}