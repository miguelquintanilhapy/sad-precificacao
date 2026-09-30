using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using magal.Data.Repositories;
using magal.Models;

namespace magal.Views
{
    public partial class CadastrarFuncionarioDialog : Window
    {
        public CadastrarFuncionarioDialog()
        {
            InitializeComponent();
            ContentRendered += (_, _) => _estadoInicial = CapturarEstado();
            Closing += ConfirmarDescarteAlteracoes;
            CarregarCargos();
        }

        private async void CarregarCargos()
        {
            var repo = new CargoRepository();
            ComboCargo.ItemsSource = await repo.ListarTodos();
        }

        private void BtnSalvar_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtNome.Text) ||
                ComboCargo.SelectedItem == null ||
                ComboNivel.SelectedItem == null ||
                ComboTipoVinculo.SelectedItem == null ||
                ComboStatus.SelectedItem == null)
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
                var cargoSelecionado = (Cargo)ComboCargo.SelectedItem;

                string nivel = ((ComboBoxItem)ComboNivel.SelectedItem)
                    .Content
                    .ToString();

                decimal custoHora = cargoSelecionado.custo_medio_hora;

                switch (nivel)
                {
                    case "Júnior":
                        custoHora = cargoSelecionado.custo_medio_hora / 1.75m;
                        break;

                    case "Pleno":
                        custoHora = cargoSelecionado.custo_medio_hora;
                        break;

                    case "Sênior":
                        custoHora = cargoSelecionado.custo_medio_hora * 1.5m;
                        break;

                    case "Especialista":
                        custoHora = cargoSelecionado.custo_medio_hora * 2m;
                        break;
                }

                var funcionario = new Funcionario
                {
                    nome = TxtNome.Text.Trim(),

                    id_cargo = cargoSelecionado.id_cargo,

                    nivel = nivel,
                  
                    tipo_vinculo = ((ComboBoxItem)ComboTipoVinculo.SelectedItem)
                        .Content
                        .ToString(),

                    status = ((ComboBoxItem)ComboStatus.SelectedItem)
                        .Content
                        .ToString()
                };

                var repo = new FuncionarioRepository();

                repo.Inserir(funcionario);

                MessageBox.Show(
                    "Funcionário cadastrado com sucesso!",
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
                ComboCargo.SelectedValue?.ToString(),
                ComboCargo.Text,
                ComboNivel.SelectedValue?.ToString(),
                ComboNivel.Text,
                ComboTipoVinculo.SelectedValue?.ToString(),
                ComboTipoVinculo.Text,
                ComboStatus.SelectedValue?.ToString(),
                ComboStatus.Text
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