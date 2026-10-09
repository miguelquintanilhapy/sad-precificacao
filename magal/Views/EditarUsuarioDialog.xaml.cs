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
            ContentRendered += (_, _) => _estadoInicial = CapturarEstado();
            Closing += ConfirmarDescarteAlteracoes;

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

        private async void BtnSalvar_Click(object sender, RoutedEventArgs e)
        {
            // Validação de todos os campos obrigatórios na tela
            if (CampoGuia.MarcarPendentes(TxtNome, TxtEmail, ComboStatus, ComboNivel))
            {
                MessageBox.Show(
                    "Preencha todos os campos obrigatórios.",
                    "Aero Concepts",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            var botaoSalvar = sender as System.Windows.Controls.Button;
            if (botaoSalvar != null) botaoSalvar.IsEnabled = false;
            var copiaOriginal = EdicaoSegura.Copiar(_usuario);
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
                await repo.Atualizar(_usuario);

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
                TratadorErros.Mostrar(ex, "salvar os dados");
                if (botaoSalvar != null) botaoSalvar.IsEnabled = true;
                EdicaoSegura.Restaurar(_usuario, copiaOriginal);
            }
        }

        // Estado dos campos ao abrir a janela, usado para detectar alterações não salvas
        private string _estadoInicial;

        private string CapturarEstado() => string.Join("\u001f", new[]
        {
                TxtNome.Text,
                TxtEmail.Text,
                TxtSenhaNova.Password,
                ComboNivel.SelectedValue?.ToString(),
                ComboNivel.Text,
                ComboStatus.SelectedValue?.ToString(),
                ComboStatus.Text
        });

        private void ConfirmarDescarteAlteracoes(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Salvo com sucesso (DialogResult = true) ou sem alterações: fecha sem perguntar
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