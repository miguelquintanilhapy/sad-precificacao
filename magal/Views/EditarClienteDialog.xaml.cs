using System;
using System.Windows;
using System.Windows.Controls;
using magal.Data.Repositories;
using magal.Models;
using magal.Services;

namespace magal.Views
{
    public partial class EditarClienteDialog : Window
    {
        private readonly Cliente _cliente;

        public EditarClienteDialog(Cliente cliente)
        {
            InitializeComponent();
            ContentRendered += (_, _) => _estadoInicial = CapturarEstado();
            Closing += ConfirmarDescarteAlteracoes;

            _cliente = cliente;

            PreencherCampos();
        }

        private void PreencherCampos()
        {
            TxtNome.Text = _cliente.nome;
            TxtCpfCnpj.Text = _cliente.cpf_cnpj;
            TxtCidade.Text = _cliente.cidade;
            TxtEstado.Text = _cliente.estado;
            TxtContato.Text = _cliente.contato;

            // TIPO
            foreach (ComboBoxItem item in ComboTipo.Items)
            {
                if (item.Content.ToString() == _cliente.tipo)
                {
                    ComboTipo.SelectedItem = item;
                    break;
                }
            }
        }

        private async void BtnSalvar_Click(object sender, RoutedEventArgs e)
        {
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
            var copiaOriginal = EdicaoSegura.Copiar(_cliente);
            try
            {
                _cliente.nome = TxtNome.Text.Trim();

                _cliente.tipo = ((ComboBoxItem)ComboTipo.SelectedItem)
                    .Content
                    .ToString();

                _cliente.cpf_cnpj = TxtCpfCnpj.Text.Trim();
                _cliente.cidade = TxtCidade.Text.Trim();
                _cliente.estado = TxtEstado.Text.Trim().ToUpper();
                _cliente.contato = TxtContato.Text.Trim();

                var repo = new ClienteRepository();
                await repo.Atualizar(_cliente);

                MessageBox.Show(
                    "Cliente atualizado com sucesso!",
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
                EdicaoSegura.Restaurar(_cliente, copiaOriginal);
            }
        }

        // Estado dos campos ao abrir a janela, usado para detectar alterações não salvas
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