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
    public class UsuariosViewModel : BaseModel
    {
        #region Atributos e Campos Privados

        private readonly UsuarioRepository _repository; // Ajuste para o nome correto do seu Repo
        private readonly PdfService _pdfService;
        private Usuario _usuarioSelecionado;
        private string _filtroTexto;
        private bool _isLoading = true;

        #endregion

        #region Propriedades e Filtros

        /// <summary>
        /// Sinaliza se a listagem de usuários está buscando dados do banco.
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
                UsuariosView?.Refresh();
            }
        }

        public Usuario UsuarioSelecionado
        {
            get => _usuarioSelecionado;
            set { _usuarioSelecionado = value; OnPropertyChanged(); }
        }

        #endregion

        #region Coleções e Visões de Dados

        public ObservableCollection<Usuario> Usuarios { get; } = new ObservableCollection<Usuario>();
        public ICollectionView UsuariosView { get; private set; }

        #endregion

        #region Comandos disparados pela View

        public RelayCommand ExcluirCommand { get; }
        public RelayCommand AtuaisCommand => AtualizarCommand; // Alias de segurança
        public RelayCommand AtualizarCommand { get; }
        public RelayCommand CriarCommand { get; }
        public RelayCommand EditarCommand { get; }
        public RelayCommand ExportarPdfCommand { get; }

        #endregion

        #region Construtores

        public UsuariosViewModel()
        {
            _repository = new UsuarioRepository(); // Instancia o repositório de usuários
            _pdfService = new PdfService();

            UsuariosView = CollectionViewSource.GetDefaultView(Usuarios);
            UsuariosView.Filter = FiltroDeUsuarios;

            ExcluirCommand = new RelayCommand(p => ExecutarExclusao(p as Usuario));
            // Sincroniza o botão Atualizar com a Task assíncrona de carregamento
            AtualizarCommand = new RelayCommand(async _ => await CarregarUsuariosAsync());
            CriarCommand = new RelayCommand(_ => ExecutarCriar());
            EditarCommand = new RelayCommand(p => ExecutarEdicao(p as Usuario));
            ExportarPdfCommand = new RelayCommand(_ => ExecutarExportacaoPdf());
        }

        #endregion

        #region Métodos Públicos

        /// <summary>
        /// Busca a lista atualizada de usuários assincronamente gerenciando o estado de Loading.
        /// </summary>
        public async Task CarregarUsuariosAsync()
        {
            try
            {
                IsLoading = true;
                FiltroTexto = string.Empty;

                var lista = await _repository.ListarTodos();
                Usuarios.Clear();
                foreach (var u in lista)
                {
                    Usuarios.Add(u);
                }

                UsuariosView?.Refresh();
                await Task.Delay(100); // Estabiliza a renderização visual do DataGrid
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar usuários: {ex.Message}", "Aero Concepts",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        #endregion

        #region Métodos Auxiliares / Privados

        private bool FiltroDeUsuarios(object obj)
        {
            if (string.IsNullOrWhiteSpace(FiltroTexto)) return true;
            if (obj is not Usuario user) return false;

            var busca = FiltroTexto.ToLower().Trim();

            // Filtra de forma inteligente por Nome, Email ou Perfil/Cargo do usuário
            return (user.nome?.ToLower().Contains(busca) ?? false) ||
                   (user.email?.ToLower().Contains(busca) ?? false) ||
                   (user.status?.ToLower().Contains(busca) ?? false);
        }

        private async void ExecutarExclusao(Usuario usuario)
        {
            if (usuario == null) return;

            //Bloqueia a exclusão de usuários para quem não é Administrador
            if (Sessao.UsuarioLogado == null || Sessao.UsuarioLogado.nivel != "Administrador")
            {
                MessageBox.Show(
                    "Acesso Negado!\nApenas usuários com o nível 'Administrador' possuem permissão para excluir usuários.",
                    "Aero Concepts - Segurança",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            var msg = $"Tem certeza que deseja excluir o usuário '{usuario.nome}'?";
            if (MessageBox.Show(msg, "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                try
                {
                    await _repository.Excluir(usuario.id_usuario);
                    Usuarios.Remove(usuario);
                    UsuariosView?.Refresh();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erro ao excluir usuário: {ex.Message}", "Erro",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void ExecutarCriar()
        {
            //Bloqueia o cadastro de usuários para quem não é Administrador
            if (Sessao.UsuarioLogado == null || Sessao.UsuarioLogado.nivel != "Administrador")
            {
                MessageBox.Show(
                    "Acesso Negado!\nApenas usuários com o nível 'Administrador' possuem permissão para cadastrar novos usuários.",
                    "Aero Concepts - Segurança",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            var dialog = new magal.Views.CadastrarUsuarioDialog();
            dialog.Owner = Application.Current.Windows.OfType<magal.MainWindow>().FirstOrDefault();
            if (dialog.ShowDialog() == true)
                await CarregarUsuariosAsync();
        }

        private async void ExecutarEdicao(Usuario usuario)
        {
            if (usuario == null) return;

            //Bloqueia a edição de usuários para quem não é Administrador
            if (Sessao.UsuarioLogado == null || Sessao.UsuarioLogado.nivel != "Administrador")
            {
                MessageBox.Show(
                    "Acesso Negado!\nApenas usuários com o nível 'Administrador' possuem permissão para editar usuários.",
                    "Aero Concepts - Segurança",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            // Certifique-se de criar o construtor aceitando o objeto Usuario na sua janela de edição
            var dialog = new magal.Views.EditarUsuarioDialog(usuario);
            dialog.Owner = Application.Current.Windows.OfType<magal.MainWindow>().FirstOrDefault();
            if (dialog.ShowDialog() == true)
                await CarregarUsuariosAsync();
        }

        private async void ExecutarExportacaoPdf()
        {
            var usuariosFiltrados = UsuariosView.Cast<Usuario>().ToList();

            if (!usuariosFiltrados.Any())
            {
                MessageBox.Show("Não há registros na tabela para exportar.", "Aero Concepts",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string pastaDownloads = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");

            var saveFileDialog = new SaveFileDialog
            {
                Filter = "Arquivos PDF (*.pdf)|*.pdf",
                FileName = $"Relatorio_Usuarios_{DateTime.Now:yyyyMMdd_HHmm}.pdf",
                InitialDirectory = System.IO.Directory.Exists(pastaDownloads) ? pastaDownloads : string.Empty,
                Title = "Salvar Relatório de Usuários"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    // Lembre-se de adicionar esse método interno no seu PdfService para renderizar usuários
                    await Task.Run(() => _pdfService.GerarRelatorioTabelaUsuarios(usuariosFiltrados, saveFileDialog.FileName));

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