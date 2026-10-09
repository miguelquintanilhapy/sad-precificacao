using System;
using System.Windows;
using System.Windows.Controls;
using magal.Data.Repositories;
using magal.Models;
using magal.Services;

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

        private async void BtnSalvar_Click(object sender, RoutedEventArgs e)
        {
            // Validação dos campos obrigatórios
            if (CampoGuia.MarcarPendentes(TxtNome, ComboTipo, TxtCpfCnpj, TxtCidade, TxtEstado, TxtContato))
            {
                MessageBox.Show(
                    "Preencha todos os campos.",
                    "Aero Concepts",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            var botaoSalvar = sender as System.Windows.Controls.Button;
            if (botaoSalvar != null) botaoSalvar.IsEnabled = false;
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
                await repo.Inserir(cliente);

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
                TratadorErros.Mostrar(ex, "salvar os dados");
                if (botaoSalvar != null) botaoSalvar.IsEnabled = true;
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