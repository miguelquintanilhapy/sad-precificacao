using magal.Data.Repositories;
using magal.Models;
using magal.Services;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace magal.ViewModels
{
    public class FuncionarioViewModel : BaseModel
    {
        #region Atributos e Campos Privados

        private readonly FuncionarioRepository _repository;
        private readonly PdfService _pdfService;
        private Funcionario _funcionarioSelecionado;
        private string _filtroTexto;
        private bool _isLoading = true;

        #endregion

        #region Propriedades e Filtros

        /// <summary>
        /// Sinaliza se a listagem de funcionários está buscando dados do banco.
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }

        public string FiltroTexto
        {
            get => _filtroTexto;
            set
            {
                _filtroTexto = value;
                OnPropertyChanged();
                FuncionariosView?.Refresh();
            }
        }

        public Funcionario FuncionarioSelecionado
        {
            get => _funcionarioSelecionado;
            set { _funcionarioSelecionado = value; OnPropertyChanged(); }
        }

        #endregion

        #region Coleções e Visões de Dados

        public ObservableCollection<Funcionario> Funcionarios { get; } = new ObservableCollection<Funcionario>();
        public ICollectionView FuncionariosView { get; private set; }

        #endregion

        #region Comandos disparados pela View

        public RelayCommand ExcluirCommand { get; }
        public RelayCommand AtuaisCommand => AtualizarCommand;
        public RelayCommand AtualizarCommand { get; }
        public RelayCommand CriarCommand { get; }
        public RelayCommand EditarCommand { get; }
        public RelayCommand ExportarPdfCommand { get; }

        #endregion

        #region Construtores

        public FuncionarioViewModel()
        {
            _repository = new FuncionarioRepository();
            _pdfService = new PdfService();

            FuncionariosView = CollectionViewSource.GetDefaultView(Funcionarios);
            FuncionariosView.Filter = FiltroDeFuncionarios;

            ExcluirCommand = new RelayCommand(p => ExecutarExclusao(p as Funcionario));
            // Sincroniza o botão Atualizar com a Task assíncrona de carregamento
            AtualizarCommand = new RelayCommand(async _ => await CarregarFuncionarios());
            CriarCommand = new RelayCommand(_ => ExecutarCriar());
            EditarCommand = new RelayCommand(p => ExecutarEdicao(p as Funcionario));
            ExportarPdfCommand = new RelayCommand(_ => ExecutarExportacaoPdf());
        }

        #endregion

        #region Métodos Públicos

        /// <summary>
        /// Busca a lista atualizada de funcionários assincronamente gerenciando o estado de Loading.
        /// </summary>
        public async Task CarregarFuncionarios()
        {
            try
            {
                IsLoading = true;
                FiltroTexto = string.Empty;

                var lista = await _repository.ListarTodos();
                Funcionarios.Clear();

                // Verifica se o usuário NÃO é um Administrador
                bool naoEAdmin = Sessao.UsuarioLogado == null || Sessao.UsuarioLogado.nivel != "Administrador";

                foreach (var f in lista)
                {
                    if (naoEAdmin)
                    {
                        f.CustoHoraExibicao = "—"; 
                    }
                    else
                    {
                        f.CustoHoraExibicao = f.custo_hora.ToString("C", new System.Globalization.CultureInfo("pt-BR"));
                    }

                    Funcionarios.Add(f);
                }

                FuncionariosView?.Refresh();
                await Task.Delay(100);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar funcionários: {ex.Message}", "Aero Concepts",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        #endregion

        #region Métodos Auxiliares / Privados

        private bool FiltroDeFuncionarios(object obj)
        {
            if (string.IsNullOrWhiteSpace(FiltroTexto)) return true;
            if (obj is not Funcionario func) return false;

            var busca = FiltroTexto.ToLower().Trim();

            return (func.nome?.ToLower().Contains(busca) ?? false) ||
                   (func.tipo_vinculo?.ToLower().Contains(busca) ?? false) ||
                   (func.status?.ToLower().Contains(busca) ?? false);
        }

        private async void ExecutarExclusao(Funcionario funcionario)
        {
            if (funcionario == null) return;

            // Bloqueia a exclusão para quem não é Administrador
            if (Sessao.UsuarioLogado == null || Sessao.UsuarioLogado.nivel != "Administrador")
            {
                MessageBox.Show(
                    "Acesso Negado!\nApenas usuários com o nível 'Administrador' possuem permissão para excluir funcionários.",
                    "Aero Concepts - Segurança",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return; // Interrompe o fluxo e protege o banco de dados
            }

            var msg = $"Tem certeza que deseja excluir '{funcionario.nome}'?\nEsta ação não poderá ser desfeita.";
            if (MessageBox.Show(msg, "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                try
                {
                    await _repository.Excluir(funcionario.id_funcionario);
                    Funcionarios.Remove(funcionario);
                    FuncionariosView?.Refresh();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erro ao excluir funcionário: {ex.Message}", "Erro",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void ExecutarCriar()
        {
            var dialog = new magal.Views.CadastrarFuncionarioDialog();
            dialog.Owner = Application.Current.Windows.OfType<magal.MainWindow>().FirstOrDefault();
            if (dialog.ShowDialog() == true)
                await CarregarFuncionarios();
        }

        private async void ExecutarEdicao(Funcionario funcionario)
        {
            if (funcionario == null) return;
            var dialog = new magal.Views.EditarFuncionarioDialog(funcionario);
            dialog.Owner = Application.Current.Windows.OfType<magal.MainWindow>().FirstOrDefault();
            if (dialog.ShowDialog() == true)
                await CarregarFuncionarios();
        }

        private async void ExecutarExportacaoPdf()
        {
            var funcionariosFiltrados = FuncionariosView.Cast<Funcionario>().ToList();

            if (!funcionariosFiltrados.Any())
            {
                MessageBox.Show("Não há registros na tabela para exportar.", "Aero Concepts",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string pastaDownloads = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");

            var saveFileDialog = new SaveFileDialog
            {
                Filter = "Arquivos PDF (*.pdf)|*.pdf",
                FileName = $"Relatorio_Funcionarios_{DateTime.Now:yyyyMMdd_HHmm}.pdf",
                InitialDirectory = System.IO.Directory.Exists(pastaDownloads) ? pastaDownloads : string.Empty,
                Title = "Salvar Relatório de Funcionários"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    await Task.Run(() => _pdfService.GerarRelatorioTabelaFuncionarios(funcionariosFiltrados, saveFileDialog.FileName));

                    MessageBox.Show("Relatório gerado com sucesso!", "Sucesso",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erro ao exportar PDF: {ex.Message}", "Erro",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        #endregion
    }
}