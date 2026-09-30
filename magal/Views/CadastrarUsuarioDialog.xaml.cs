using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using magal.Data.Repositories;
using magal.Models;
using magal.Services;

namespace magal.Views
{
    public partial class CadastrarUsuarioDialog : Window
    {
        public CadastrarUsuarioDialog()
        {
            InitializeComponent();
            ContentRendered += (_, _) => _estadoInicial = CapturarEstado();
            Closing += ConfirmarDescarteAlteracoes;
        }

        private async void BtnSalvar_Click(object sender, RoutedEventArgs e)
        {
            // Verifica se os campos não estão em branco (incluindo o ComboNivel)
            if (string.IsNullOrWhiteSpace(TxtNome.Text) ||
                string.IsNullOrWhiteSpace(TxtEmail.Text) ||
                string.IsNullOrWhiteSpace(TxtSenha.Password) ||
                string.IsNullOrWhiteSpace(TxtConfirmarSenha.Password) ||
                ComboNivel.SelectedItem == null)
            {
                MessageBox.Show(
                    "Por favor, preencha todos os campos.",
                    "Aero Concepts",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            // Validação do formato do e-mail via Regex
            if (!ValidarEmail(TxtEmail.Text))
            {
                MessageBox.Show(
                    "Por favor, insira um e-mail válido (exemplo@dominio.com).",
                    "E-mail Inválido",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            // Verifica se as senhas são iguais
            if (TxtSenha.Password != TxtConfirmarSenha.Password)
            {
                MessageBox.Show(
                    "As senhas digitadas não coincidem. Por favor, verifique.",
                    "Senhas Diferentes",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            try
            {
                var novoUsuario = new Usuario
                {
                    nome = TxtNome.Text.Trim(),
                    email = TxtEmail.Text.Trim(),
                    senha = PasswordHasher.Hash(TxtSenha.Password),
                    status = "Ativo", // Todo usuário entra como ativo por padrão
                    nivel = ((ComboBoxItem)ComboNivel.SelectedItem).Content.ToString()
                };

                var repo = new UsuarioRepository();
                // Chamada assíncrona utilizando o await com base no seu Repository
                await repo.Inserir(novoUsuario);

                MessageBox.Show(
                    "Usuário cadastrado com sucesso!",
                    "Aero Concepts",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Erro ao salvar no banco: " + ex.Message,
                    "Aero Concepts",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Método auxiliar para checar se o formato do e-mail é aceitável
        /// </summary>
        private bool ValidarEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            string modeloRegex = @"^[^@\s]+@[^@\s]+\.[a-zA-Z]{2,}$";

            return Regex.IsMatch(email, modeloRegex);
        }

        // Estado dos campos ao abrir a janela, usado para detectar dados digitados e não salvos
        private string _estadoInicial;

        private string CapturarEstado() => string.Join("\u001f", new[]
        {
                TxtNome.Text,
                TxtEmail.Text,
                TxtSenha.Password,
                TxtConfirmarSenha.Password,
                ComboNivel.SelectedValue?.ToString(),
                ComboNivel.Text
        });

        private void ConfirmarDescarteAlteracoes(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Salvo com sucesso (DialogResult = true) ou nada preenchido: fecha sem perguntar
            if (_estadoInicial == null || DialogResult == true || CapturarEstado() == _estadoInicial) return;

            var resposta = MessageBox.Show(
                "Existem alterações não salvas. Deseja descartá-las?",
                "Descartar alterações",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                MessageBoxResult.No);

            if (resposta != MessageBoxResult.Yes)
                e.Cancel = true;
        }
    }
}