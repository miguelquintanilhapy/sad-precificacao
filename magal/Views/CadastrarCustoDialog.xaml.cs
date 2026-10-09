using System;
using System.Windows;
using System.Windows.Controls;
using magal.Data.Repositories;
using magal.Models;
using magal.Services;

namespace magal.Views
{
    public partial class CadastrarCustoDialog : Window
    {
        public CadastrarCustoDialog()
        {
            InitializeComponent();
            ContentRendered += (_, _) => _estadoInicial = CapturarEstado();
            Closing += ConfirmarDescarteAlteracoes;
        }

        private async void BtnSalvar_Click(object sender, RoutedEventArgs e)
        {
            if (CampoGuia.MarcarPendentes(TxtNome, ComboCategoria, TxtValor))
            {
                MessageBox.Show(
                    "Preencha todos os campos.",
                    "Aero Concepts",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(TxtValor.Text.Trim(), out decimal valorConvertido) || valorConvertido < 0)
            {
                MessageBox.Show(
                    "Por favor, insira um valor numérico válido e positivo.",
                    "Aero Concepts",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            var botaoSalvar = sender as System.Windows.Controls.Button;
            if (botaoSalvar != null) botaoSalvar.IsEnabled = false;
            try
            {
                var novoCustoItem = new CatalogoCusto
                {
                    nome = TxtNome.Text.Trim(),
                    categoria = ((ComboBoxItem)ComboCategoria.SelectedItem).Content.ToString(),
                    valor = valorConvertido
                };

                var repo = new CatalogoCustoRepository();
                await repo.Inserir(novoCustoItem);

                MessageBox.Show(
                    "Item adicionado ao catálogo com sucesso!",
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
                TxtValor.Text,
                ComboCategoria.SelectedValue?.ToString(),
                ComboCategoria.Text
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