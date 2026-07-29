using System;
using System.Windows;
using magal.Data.Repositories;
using magal.Services;

namespace magal.Views
{
    public partial class MeuPerfilDialog : Window
    {
        private readonly UsuarioRepository _repository = new UsuarioRepository();

        public MeuPerfilDialog()
        {
            InitializeComponent();

            TxtNomeUsuario.Text = Sessao.UsuarioLogado?.nome ?? "Usuário sem Nome";
            TxtEmailUsuario.Text = Sessao.UsuarioLogado?.email ?? "Sem e-mail cadastrado";
        }

        private async void BtnSalvar_Click(object sender, RoutedEventArgs e)
        {
            if (Sessao.UsuarioLogado == null)
            {
                MessageBox.Show("Sessão inválida. Faça login novamente.", "Aero Concepts",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (string.IsNullOrWhiteSpace(TxtSenhaAtual.Password) ||
                string.IsNullOrWhiteSpace(TxtSenhaNova.Password) ||
                string.IsNullOrWhiteSpace(TxtConfirmarSenhaNova.Password))
            {
                MessageBox.Show("Preencha todos os campos.", "Aero Concepts",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!PasswordHasher.Verify(TxtSenhaAtual.Password, Sessao.UsuarioLogado.senha))
            {
                MessageBox.Show("A senha atual informada está incorreta.", "Aero Concepts",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (TxtSenhaNova.Password != TxtConfirmarSenhaNova.Password)
            {
                MessageBox.Show("A nova senha e a confirmação não coincidem.", "Aero Concepts",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                string novoHash = PasswordHasher.Hash(TxtSenhaNova.Password);
                await _repository.AtualizarSenha(Sessao.UsuarioLogado.id_usuario, novoHash);
                Sessao.UsuarioLogado.senha = novoHash;

                MessageBox.Show("Senha alterada com sucesso!", "Aero Concepts",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao alterar senha: {ex.Message}", "Erro",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
