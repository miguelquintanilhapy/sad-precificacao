using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using magal.Data.Repositories;
using magal.Models;
using magal.ViewModels;
using magal.Views;

namespace magal
{
    public partial class MainWindow : Window
    {
        private HomeView _homeView;
        private HistoricoView _historicoView;
        private OrcamentoView _orcamentoView;

        public MainWindow()
        {
            InitializeComponent();
            this.WindowState = WindowState.Maximized;

            CarregarDadosUsuario();
            AbrirHome();
        }

        private void CarregarDadosUsuario()
        {
            // Verifica se a sessão global existe
            if (Sessao.UsuarioLogado != null)
            {
                string nome = Sessao.UsuarioLogado.nome;
                string email = Sessao.UsuarioLogado.email;

                TxtNomeUsuarioSidebar.Text = !string.IsNullOrWhiteSpace(nome) ? nome : "Usuário sem Nome";
                TxtEmailUsuarioSidebar.Text = !string.IsNullOrWhiteSpace(email) ? email : "Sem e-mail cadastrado";

                // Atualiza a inicial do Avatar
                if (!string.IsNullOrWhiteSpace(nome))
                {
                    TxtIniciaisAvatar.Text = nome.Trim().Substring(0, 1).ToUpper();
                }
                else
                {
                    TxtIniciaisAvatar.Text = "U";
                }
            }
            else
            {
                // Fallback caso a sessão falhe por algum motivo
                TxtNomeUsuarioSidebar.Text = "Acesso Convidado";
                TxtEmailUsuarioSidebar.Text = "Sessão não identificada";
                TxtIniciaisAvatar.Text = "?";
            }
        }

        // --- NAVEGAÇÃO ---
        private void BtnHome_Click(object sender, RoutedEventArgs e) => AbrirHome();
        private void BtnOrcamentos_Click(object sender, RoutedEventArgs e) => AbrirOrcamento();
        private void BtnHistorico_Click(object sender, RoutedEventArgs e) => AbrirHistorico();
        private void BtnGerenciamento_Click(object sender, RoutedEventArgs e) => AbrirGerenciamento();

        private void AtualizarBotaoAtivo(Button botaoAtivo)
        {
            // Lista com todos os seus botões da sidebar
            var botoes = new[] { BtnHome, BtnOrcamentos, BtnHistorico, BtnDashboard, BtnGerenciamento };

            foreach (var btn in botoes)
            {
                if (btn == null) continue;

                if (btn == botaoAtivo)
                {
                    // Define uma Tag que avisa ao XAML para fixar o estilo ativo
                    btn.Tag = "Ativo";
                }
                else
                {
                    btn.Tag = null;
                }
            }
        }

        public void AbrirHome()
        {
            if (_homeView == null) _homeView = new HomeView();
            MainContent.Content = _homeView;
            AtualizarBotaoAtivo(BtnHome); 
        }

        public void AbrirOrcamento()
        {
            _orcamentoView = new OrcamentoView();
            MainContent.Content = _orcamentoView;
            AtualizarBotaoAtivo(BtnOrcamentos); 
        }

        public void AbrirHistorico()
        {
            if (_historicoView == null) _historicoView = new HistoricoView();
            MainContent.Content = _historicoView;
            AtualizarBotaoAtivo(BtnHistorico);
        }

        private void BtnDashboard_Click(object sender, RoutedEventArgs e)
        {
            //Impede usuários comuns de visualizarem o Dashboard
            if (Sessao.UsuarioLogado == null || Sessao.UsuarioLogado.nivel != "Administrador")
            {
                MessageBox.Show(
                    "Acesso Restrito!\nApenas usuários com o nível 'Administrador' possuem permissão para acessar o Dashboard.",
                    "Aero Concepts - Segurança",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return; // Interrompe a execução aqui e não altera a tela
            }

            // Se for Admin, segue o fluxo normal
            MainContent.Content = new GraficoHistoricoView();
            AtualizarBotaoAtivo(BtnDashboard);
        }

        public void AbrirGerenciamento()
        {
            MainContent.Content = new GerenciamentoView();
            AtualizarBotaoAtivo(BtnGerenciamento); 
        }

        public async void IrParaEdicao(Projeto projetoSimplificado)
        {
            var repo = new ProjetoRepository();
            Projeto projetoCompleto = await repo.CarregarProjetoCompleto(projetoSimplificado.id_projeto);

            var viewModel = new OrcamentoViewModel();
            viewModel.CarregarProjetoParaEdicao(projetoCompleto);

            var view = new OrcamentoView();
            view.DataContext = viewModel;
            MainContent.Content = view;

            AtualizarBotaoAtivo(BtnOrcamentos); 
        }

        // --- PERFIL E USUÁRIO ---
        private void BtnPerfil_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.ContextMenu != null)
            {
                btn.ContextMenu.PlacementTarget = btn;
                btn.ContextMenu.IsOpen = true;
            }
        }

        private void MenuMeuPerfil_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new MeuPerfilDialog();
            dialog.Owner = this;
            dialog.ShowDialog();
        }

        private void MenuCadastrarUsuario_Click(object sender, RoutedEventArgs e)
        {
            //Impede usuários comuns de cadastrarem novos usuários
            if (Sessao.UsuarioLogado == null || Sessao.UsuarioLogado.nivel != "Administrador")
            {
                MessageBox.Show(
                    "Acesso Restrito!\nApenas administradores possuem permissão para cadastrar novos usuários.",
                    "Aero Concepts - Segurança",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return; // Bloqueia a abertura 
            }

            CadastrarUsuarioDialog dialog = new CadastrarUsuarioDialog();
            dialog.Owner = this;
            dialog.ShowDialog();
        }

        private void MenuVisualizarUsuarios_Click(object sender, RoutedEventArgs e)
        {
            //Impede usuários comuns de visualizarem a lista de usuários
            if (Sessao.UsuarioLogado == null || Sessao.UsuarioLogado.nivel != "Administrador")
            {
                MessageBox.Show(
                    "Acesso Restrito!\nApenas administradores possuem permissão para gerenciar e visualizar usuários.",
                    "Aero Concepts - Segurança",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return; // Bloqueia a troca de tela
            }

            MainContent.Content = new UsuariosListaView();
            AtualizarBotaoAtivo(BtnGerenciamento);
        }

        private void MenuLogoff_Click(object sender, RoutedEventArgs e)
        {
            var resultado = MessageBox.Show("Deseja realmente sair e voltar para o login?", "LogOut",
                                            MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (resultado == MessageBoxResult.Yes)
            {
                Sessao.UsuarioLogado = null; 
                LoginView login = new LoginView();
                login.Show();
                Close(); 
            }
        }

        public ContentControl MainContentControl => MainContent;
    }
}
