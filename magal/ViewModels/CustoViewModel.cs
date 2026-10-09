using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.ComponentModel;
using System.Windows.Data;
using System.Threading.Tasks;
using System.Collections.Generic;
using magal.Models;
using magal.Data.Repositories;
using magal.Services;
using Microsoft.Win32;

namespace magal.ViewModels
{
    public class CustoViewModel : BaseModel
    {
        #region Atributos e Campos Privados

        private readonly CatalogoCustoRepository _repository;
        private readonly PdfService _pdfService;
        private CatalogoCusto _custoSelecionado;
        private string _filtroTexto;
        private bool _isLoading;

        #endregion

        #region Propriedades e Filtros

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (_isLoading != value)
                {
                    _isLoading = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsNotLoading));

                    // Força o WPF a reavaliar automaticamente o CanExecute de todos os botões vinculados
                    System.Windows.Input.CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public bool IsNotLoading => !IsLoading;

        public string FiltroTexto
        {
            get => _filtroTexto;
            set
            {
                _filtroTexto = value;
                OnPropertyChanged();
                CustosView?.Refresh();
            }
        }

        public CatalogoCusto CustoSelecionado
        {
            get => _custoSelecionado;
            set { _custoSelecionado = value; OnPropertyChanged(); }
        }

        #endregion

        #region Coleções e Visões de Dados

        public ObservableCollection<CatalogoCusto> Custos { get; } = new ObservableCollection<CatalogoCusto>();
        public ICollectionView CustosView { get; private set; }

        #endregion

        #region Comandos disparados pela View

        public RelayCommand ExcluirCommand { get; }
        public RelayCommand AtualizarCommand { get; }
        public RelayCommand CriarCommand { get; }
        public RelayCommand EditarCommand { get; }
        public RelayCommand ExportarPdfCommand { get; }
        public RelayCommand VoltarCommand { get; }

        #endregion

        #region Construtor

        public CustoViewModel()
        {
            _repository = new CatalogoCustoRepository();
            _pdfService = new PdfService();

            CustosView = CollectionViewSource.GetDefaultView(Custos);
            CustosView.Filter = FiltroDeCustos;

            // Inicialização dos Comandos avaliando a trava de carregamento do sistema
            ExcluirCommand = new RelayCommand(async p => await ExecutarExclusao(p as CatalogoCusto), _ => IsNotLoading);
            AtualizarCommand = new RelayCommand(async _ => await CarregarCustos(), _ => IsNotLoading);
            CriarCommand = new RelayCommand(_ => ExecutarCriar(), _ => IsNotLoading);
            EditarCommand = new RelayCommand(_ => ExecutarEdicao(CustoSelecionado), _ => IsNotLoading);
            ExportarPdfCommand = new RelayCommand(async _ => await ExecutarExportacaoPdf(), _ => IsNotLoading);
            VoltarCommand = new RelayCommand(_ => ExecutarVoltar());

            // Inicializa a tela trazendo a listagem
            _ = CarregarCustos();
        }

        #endregion

        #region Métodos de Ação

        public async Task CarregarCustos()
        {
            if (IsLoading) return;

            try
            {
                IsLoading = true;
                FiltroTexto = string.Empty;

                var lista = await Task.Run(() => _repository.ListarTodos());

                Custos.Clear();
                foreach (var c in lista)
                {
                    Custos.Add(c);
                }

                CustosView?.Refresh();
            }
            catch (Exception ex)
            {
                TratadorErros.Mostrar(ex, "carregar o catálogo de custos");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task ExecutarExclusao(CatalogoCusto custo)
        {
            if (custo == null || IsLoading) return;

            //Bloqueia a exclusão para quem não é Administrador
            if (Sessao.UsuarioLogado == null || Sessao.UsuarioLogado.nivel != "Administrador")
            {
                MessageBox.Show(
                    "Acesso Negado!\nApenas usuários com o nível 'Administrador' possuem permissão para excluir custos do catálogo.",
                    "Aero Concepts - Segurança",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return; // Interrompe o fluxo e protege os dados
            }

            var msg = $"Tem certeza que deseja excluir o custo '{custo.nome}' no valor de {custo.valor:C} do catálogo?\nEsta ação não poderá ser desfeita.";
            if (MessageBox.Show(msg, "Confirmar Exclusão", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                try
                {
                    IsLoading = true;
                    await Task.Run(() => _repository.Excluir(custo.id_catalogo_custo));
                    Custos.Remove(custo);
                    CustosView?.Refresh();
                }
                catch (Exception ex)
                {
                    TratadorErros.Mostrar(ex, "excluir o custo");
                }
                finally
                {
                    IsLoading = false;
                }
            }
        }

        private void ExecutarCriar()
        {
            // Bloqueia a criação de custos para quem não é Administrador
            if (Sessao.UsuarioLogado == null || Sessao.UsuarioLogado.nivel != "Administrador")
            {
                MessageBox.Show(
                    "Acesso Negado!\nApenas usuários com o nível 'Administrador' possuem permissão para cadastrar novos custos.",
                    "Aero Concepts - Segurança",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return; // Interrompe o fluxo e impede a abertura da tela de cadastro
            }

            var dialog = new magal.Views.CadastrarCustoDialog();
            dialog.Owner = Application.Current.Windows.OfType<magal.MainWindow>().FirstOrDefault();
            if (dialog.ShowDialog() == true)
            {
                _ = CarregarCustos();
            }
        }

        private void ExecutarEdicao(CatalogoCusto custo)
        {
            if (custo == null) return;

            // Bloqueia a edição de custos para quem não é Administrador
            if (Sessao.UsuarioLogado == null || Sessao.UsuarioLogado.nivel != "Administrador")
            {
                MessageBox.Show(
                    "Acesso Negado!\nApenas usuários com o nível 'Administrador' possuem permissão para alterar os custos operacionais.",
                    "Aero Concepts - Segurança",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return; // Interrompe o fluxo e protege os dados de custos
            }

            var dialog = new magal.Views.EditarCustoDialog(custo);
            dialog.Owner = Application.Current.Windows.OfType<magal.MainWindow>().FirstOrDefault();
            if (dialog.ShowDialog() == true)
            {
                _ = CarregarCustos();
            }
        }

        private async Task ExecutarExportacaoPdf()
        {
            if (IsLoading) return;

            var custosFiltrados = CustosView.Cast<CatalogoCusto>().ToList();

            if (!custosFiltrados.Any())
            {
                MessageBox.Show("Não há registros na tabela para exportar.", "Aero Concepts",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string pastaDownloads = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");

            var saveFileDialog = new SaveFileDialog
            {
                Filter = "Arquivos PDF (*.pdf)|*.pdf",
                FileName = $"Relatorio_Catalogo_Custos_{DateTime.Now:yyyyMMdd_HHmm}.pdf",
                InitialDirectory = System.IO.Directory.Exists(pastaDownloads) ? pastaDownloads : string.Empty,
                Title = "Salvar Relatório do Catálogo de Custos"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    IsLoading = true;
                    string caminhoSalvar = saveFileDialog.FileName;

                    await Task.Run(() => _pdfService.GerarRelatorioTabelaCustos(custosFiltrados, caminhoSalvar));

                    MessageBox.Show("Relatório do catálogo de custos gerado com sucesso!", "Sucesso",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    TratadorErros.Mostrar(ex, "exportar o PDF");
                }
                finally
                {
                    IsLoading = false;
                }
            }
        }

        private void ExecutarVoltar()
        {
            var mainWindow = Application.Current.Windows.OfType<magal.MainWindow>().FirstOrDefault();
            mainWindow?.AbrirGerenciamento();
        }

        private bool FiltroDeCustos(object obj)
        {
            if (string.IsNullOrWhiteSpace(FiltroTexto)) return true;
            if (obj is not CatalogoCusto custo) return false;

            var busca = FiltroTexto.ToLower().Trim();

            return (custo.nome?.ToLower().Contains(busca) ?? false) ||
                   (custo.categoria?.ToLower().Contains(busca) ?? false);
        }

        #endregion
    }
}