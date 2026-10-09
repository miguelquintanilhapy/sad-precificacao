using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using magal.Data.Repositories;
using magal.Models;
using magal.Services;
using System.Linq;

namespace magal.Views
{
    public partial class LoginView : Window
    {
        private readonly UsuarioRepository _repository = new UsuarioRepository();
        private bool senhaVisivel = false;

        public LoginView()
        {
            InitializeComponent();
            // Garante que o foco inicial esteja no e-mail
            txtUsuario.Focus();
        }

        #region Lógica de Interface (Caps Lock e Visibilidade)

        private void TxtSenha_PreviewKeyDown(object sender, KeyEventArgs e) => VerificarCapsLock();
        private void TxtSenha_PreviewKeyUp(object sender, KeyEventArgs e) => VerificarCapsLock();
        private void TxtSenha_GotFocus(object sender, RoutedEventArgs e) => VerificarCapsLock();

        private void VerificarCapsLock()
        {
            pnlCapsWarning.Visibility = Keyboard.IsKeyToggled(Key.CapsLock) ? Visibility.Visible : Visibility.Collapsed;
        }

        private void BtnMostrarSenha_Click(object sender, RoutedEventArgs e)
        {
            senhaVisivel = !senhaVisivel;

            if (senhaVisivel)
            {
                txtSenhaVisivel.Text = txtSenha.Password;
                txtSenha.Visibility = Visibility.Collapsed;
                txtSenhaVisivel.Visibility = Visibility.Visible;
                riscoOlhoFechado.Visibility = Visibility.Collapsed;
                txtSenhaVisivel.Focus();
                txtSenhaVisivel.CaretIndex = txtSenhaVisivel.Text.Length;
            }
            else
            {
                txtSenha.Password = txtSenhaVisivel.Text;
                txtSenhaVisivel.Visibility = Visibility.Collapsed;
                txtSenha.Visibility = Visibility.Visible;
                riscoOlhoFechado.Visibility = Visibility.Visible;
                txtSenha.Focus();
            }
        }
        #endregion

        #region Lógica de Negócio (Autenticação)

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string usuarioDigitado = txtUsuario.Text.Trim();
            string senhaDigitada = senhaVisivel ? txtSenhaVisivel.Text : txtSenha.Password;

            if (string.IsNullOrWhiteSpace(usuarioDigitado) || string.IsNullOrWhiteSpace(senhaDigitada))
            {
                MessageBox.Show("Por favor, informe suas credenciais.", "Aero Concepts", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (LoginThrottle.EstaBloqueado(usuarioDigitado, out TimeSpan tempoRestante))
            {
                MessageBox.Show(
                    $"Muitas tentativas de login incorretas.\nTente novamente em {Math.Ceiling(tempoRestante.TotalSeconds)} segundos.",
                    "Acesso Temporariamente Bloqueado",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            try
            {
                Usuario usuarioLogado = await _repository.BuscarPorEmailAtivo(usuarioDigitado);

                // A senha é validada em código (hash), não mais comparada diretamente no SQL.
                if (usuarioLogado != null && PasswordHasher.Verify(senhaDigitada, usuarioLogado.senha))
                {
                    LoginThrottle.RegistrarSucesso(usuarioDigitado);

                    // Migração transparente: se a senha ainda estava em texto puro, regrava como hash.
                    if (!PasswordHasher.EhHashV1(usuarioLogado.senha))
                    {
                        string novoHash = PasswordHasher.Hash(senhaDigitada);
                        await _repository.AtualizarSenha(usuarioLogado.id_usuario, novoHash);
                        usuarioLogado.senha = novoHash;
                    }

                    magal.Sessao.UsuarioLogado = usuarioLogado;
                    ExecutarAnimacaoSaida();
                }
                else
                {
                    LoginThrottle.RegistrarFalha(usuarioDigitado);
                    MessageBox.Show("Usuário não encontrado ou senha incorreta.", "Erro de Acesso", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                TratadorErros.Mostrar(ex, "entrar no sistema");
            }
        }

        private void ExecutarAnimacaoSaida()
        {
            var fadeOut = new DoubleAnimation
            {
                From = 1.0,
                To = 0.0,
                Duration = TimeSpan.FromSeconds(0.4),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            fadeOut.Completed += (s, a) =>
            {
                MainWindow principal = new MainWindow();
                principal.Show();
                this.Close();
            };

            MainGrid.BeginAnimation(UIElement.OpacityProperty, fadeOut);
        }

        private void SenhaButton_Click(object sender, MouseButtonEventArgs e)
        {
            MessageBox.Show("Para redefinir sua senha, entre em contato com o suporte de TI da Aero Concepts.", "Recuperação de Acesso");
        }
        #endregion
    }
}