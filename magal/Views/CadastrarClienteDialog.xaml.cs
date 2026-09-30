using System;
using System.Windows;
using System.Windows.Controls;
using magal.Data.Repositories;
using magal.Models;

namespace magal.Views
{
    public partial class CadastrarClienteDialog : Window
    {
        public CadastrarClienteDialog()
        {
            InitializeComponent();
            ContentRendered += (_, _) => _estadoInicial = CapturarEstado();
            Closing += ConfirmarDescarteAlteracoes;
        }

        private void BtnSalvar_Click(object sender, RoutedEventArgs e)
        {
            // Validação dos campos obrigatórios
            if (string.IsNullOrWhiteSpace(TxtNome.Text) ||
                ComboTipo.SelectedItem == null ||
                string.IsNullOrWhiteSpace(TxtCpfCnpj.Text) ||
                string.IsNullOrWhiteSpace(TxtCidade.Text) ||
                string.IsNullOrWhiteSpace(TxtEstado.Text) ||
                string.IsNullOrWhiteSpace(TxtContato.Text))
            {
                MessageBox.Show(
                    "Preencha todos os campos.",
                    "Aero Concepts",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            try
            {
                // Instancia o modelo com as informações da tela
                var cliente = new Cliente
                {
                    nome = TxtNome.Text.Trim(),

                    tipo = ((ComboBoxItem)ComboTipo.SelectedItem).Content.ToString(),

                    cpf_cnpj = TxtCpfCnpj.Text.Trim(),
                    cidade = TxtCidade.Text.Trim(),
                    estado = TxtEstado.Text.Trim().ToUpper(), // Força sigla do estado em maiúsculo
                    contato = TxtContato.Text.Trim()
                };

                // Executa a persistência através do repositório de clientes
                var repo = new ClienteRepository();
                repo.Inserir(cliente);

                MessageBox.Show(
                    "Cliente cadastrado com sucesso!",
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

        // Estado dos campos ao abrir a janela, usado para detectar dados digitados e não salvos
        private string _estadoInicial;

        private string CapturarEstado() => string.Join("\u001f", new[]
        {
                TxtNome.Text,
                TxtCpfCnpj.Text,
                TxtCidade.Text,
                TxtEstado.Text,
                TxtContato.Text,
                ComboTipo.SelectedValue?.ToString(),
                ComboTipo.Text
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