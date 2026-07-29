using System;
using System.Windows;
using System.Windows.Controls;
using magal.Data.Repositories;
using magal.Models;
using magal.Services;

namespace magal.Views
{
    public partial class EditarUsuarioDialog : Window
    {
        private readonly Usuario _usuario;

        public EditarUsuarioDialog(Usuario usuario)
        {
            InitializeComponent();

            _usuario = usuario;

            PreencherCampos();
        }

        private void PreencherCampos()
        {
            TxtNome.Text = _usuario.nome;
            TxtEmail.Text = _usuario.email;

            // STATUS
            foreach (ComboBoxItem item in ComboStatus.Items)
            {
                if (item.Content.ToString() == _usuario.status)
                {
                    ComboStatus.SelectedItem = item;
                    break;
                }
            }

            // NÍVEL DE ACESSO
            if (ComboNivel != null && !string.IsNullOrEmpty(_usuario.nivel))
            {
                foreach (ComboBoxItem item in ComboNivel.Items)
                {
                    if (item.Content.ToString() == _usuario.nivel)
                    {
                        ComboNivel.SelectedItem = item;
                        break;
                    }
                }
            }
        }

        private void BtnSalvar_Click(object sender, RoutedEventArgs e)
        {
            // Validação de todos os campos obrigatórios na tela
            if (string.IsNullOrWhiteSpace(TxtNome.Text) ||
                string.IsNullOrWhiteSpace(TxtEmail.Text) ||
                string.IsNullOrWhiteSpace(TxtSenhaAtual.Password) ||
                ComboStatus.SelectedItem == null ||
                ComboNivel.SelectedItem == null)
            {
                MessageBox.Show(
                    "Preencha todos os campos obrigatórios.",
                    "Aero Concepts",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            // Validação de segurança: A senha digitada confere com a gravada no banco
            if (!PasswordHasher.Verify(TxtSenhaAtual.Password, _usuario.senha))
            {
                MessageBox.Show(
                    "A senha atual informada está incorreta.",
                    "Aero Concepts",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return;
            }

            try
            {
                // Atualiza as propriedades do objeto model
                _usuario.nome = TxtNome.Text.Trim();
                _usuario.email = TxtEmail.Text.Trim();

                _usuario.status = ((ComboBoxItem)ComboStatus.SelectedItem)
                    .Content
                    .ToString();

                _usuario.nivel = ((ComboBoxItem)ComboNivel.SelectedItem)
                    .Content
                    .ToString();

                // Se uma nova senha foi definida, atualiza o campo
                if (!string.IsNullOrWhiteSpace(TxtSenhaNova.Password))
                {
                    _usuario.senha = PasswordHasher.Hash(TxtSenhaNova.Password);
                }

                // Persistência no banco de dados através do repositório
                var repo = new UsuarioRepository();
                repo.Atualizar(_usuario);

                MessageBox.Show(
                    "Usuário atualizado com sucesso!",
                    "Aero Concepts",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Erro ao salvar: " + ex.Message,
                    "Aero Concepts",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}