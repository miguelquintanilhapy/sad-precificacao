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
using magal.Services;

namespace magal.Views
{
    public partial class EditarFuncionarioDialog : Window
    {
        private readonly Funcionario _funcionario;

        public EditarFuncionarioDialog(Funcionario funcionario)
        {
            InitializeComponent();
            ContentRendered += (_, _) => _estadoInicial = CapturarEstado();
            Closing += ConfirmarDescarteAlteracoes;
            ComboCargo.SelectionChanged += (_, _) =>
            {
                // Cargos são carregados de forma assíncrona: o valor inicial é o primeiro não nulo
                if (_cargoDefinido || ComboCargo.SelectedValue == null) return;
                _cargoDefinido = true;
                if (_estadoInicial != null) _estadoInicial = CapturarEstado();
            };

            _funcionario = funcionario;

            CarregarCargos();

            PreencherCampos();
        }

        private async void CarregarCargos()
        {
            try
            {
                var repo = new CargoRepository();
                ComboCargo.ItemsSource = await repo.ListarTodos();
            }
            catch (Exception ex)
            {
                TratadorErros.Mostrar(ex, "carregar os cargos");
            }
        }

        private void PreencherCampos()
        {
            TxtNome.Text = _funcionario.nome;
            
            ComboCargo.SelectedValue = _funcionario.id_cargo;

            // NÍVEL
            foreach (ComboBoxItem item in ComboNivel.Items)
            {
                if (item.Content.ToString() == _funcionario.nivel)
                {
                    ComboNivel.SelectedItem = item;
                    break;
                }
            }

            // TIPO VÍNCULO
            foreach (ComboBoxItem item in ComboTipoVinculo.Items)
            {
                if (item.Content.ToString() == _funcionario.tipo_vinculo)
                {
                    ComboTipoVinculo.SelectedItem = item;
                    break;
                }
            }

            // STATUS
            foreach (ComboBoxItem item in ComboStatus.Items)
            {
                if (item.Content.ToString() == _funcionario.status)
                {
                    ComboStatus.SelectedItem = item;
                    break;
                }
            }
        }

        private async void BtnSalvar_Click(object sender, RoutedEventArgs e)
        {
            if (CampoGuia.MarcarPendentes(TxtNome, ComboCargo, ComboNivel, ComboTipoVinculo, ComboStatus))
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
            var copiaOriginal = EdicaoSegura.Copiar(_funcionario);
            try
            {
                _funcionario.nome = TxtNome.Text.Trim();

                _funcionario.id_cargo = (int)ComboCargo.SelectedValue;

                _funcionario.nivel = ((ComboBoxItem)ComboNivel.SelectedItem)
                    .Content
                    .ToString();
            
                _funcionario.tipo_vinculo = ((ComboBoxItem)ComboTipoVinculo.SelectedItem)
                    .Content
                    .ToString();

                _funcionario.status = ((ComboBoxItem)ComboStatus.SelectedItem)
                    .Content
                    .ToString();

                var repo = new FuncionarioRepository();

                await repo.Atualizar(_funcionario);

                MessageBox.Show(
                    "Funcionário atualizado com sucesso!",
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
                EdicaoSegura.Restaurar(_funcionario, copiaOriginal);
            }
        }

        // Estado dos campos ao abrir a janela, usado para detectar alterações não salvas
        private string _estadoInicial;
        private bool _cargoDefinido;

        private string CapturarEstado() => string.Join("\u001f", new[]
        {
                TxtNome.Text,
                ComboNivel.SelectedValue?.ToString(),
                ComboNivel.Text,
                ComboTipoVinculo.SelectedValue?.ToString(),
                ComboTipoVinculo.Text,
                ComboStatus.SelectedValue?.ToString(),
                ComboStatus.Text,
                _cargoDefinido ? ComboCargo.SelectedValue?.ToString() : null
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