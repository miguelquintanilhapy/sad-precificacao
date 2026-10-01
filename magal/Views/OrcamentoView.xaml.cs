using magal.Models;
using magal.ViewModels;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace magal.Views
{
    public partial class OrcamentoView : UserControl
    {
        public OrcamentoViewModel ViewModel => this.DataContext as OrcamentoViewModel;

        #region Construtores

        public OrcamentoView()
        {
            InitializeComponent();

            var vm = new OrcamentoViewModel();
            this.DataContext = vm;

            vm.PropertyChanged += ViewModel_PropertyChanged;
            vm.CamposObrigatoriosPendentes += MarcarCamposPendentes;
        }

        public OrcamentoView(Projeto projetoParaEditar)
        {
            InitializeComponent();

            if (LoadingOverlay != null)
            {
                LoadingOverlay.Visibility = Visibility.Visible;
            }

            var vm = new OrcamentoViewModel();
            this.DataContext = vm;

            vm.PropertyChanged += ViewModel_PropertyChanged;
            vm.CamposObrigatoriosPendentes += MarcarCamposPendentes;
            vm.IsLoading = true;
            this.Loaded += (s, e) =>
            {
                Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, new System.Action(() =>
                {
                    vm.CarregarProjetoParaEdicao(projetoParaEditar);
                }));
            };
        }

        #endregion

        #region Controle do Carregamento (Idêntico ao Histórico)

        private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(OrcamentoViewModel.IsLoading) && sender is OrcamentoViewModel vm)
            {
                Dispatcher.Invoke(() =>
                {
                    if (vm.IsLoading)
                    {
                        if (LoadingOverlay != null) LoadingOverlay.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        if (LoadingOverlay != null) LoadingOverlay.Visibility = Visibility.Collapsed;
                    }
                });
            }
        }

        #endregion

        #region Campos obrigatórios

        private static readonly System.Windows.Media.Brush BrushErro =
            new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xDC, 0x26, 0x26));

        /// <summary>Destaca em vermelho os campos pendentes e leva o cursor ao primeiro deles.</summary>
        private void MarcarCamposPendentes(System.Collections.Generic.IReadOnlyList<string> campos)
        {
            if (campos.Contains("nome")) NomeBorder.BorderBrush = BrushErro;
            if (campos.Contains("cliente")) ClienteErroBorder.Visibility = Visibility.Visible;

            if (campos.Contains("nome")) NomeTextBox.Focus();
            else if (campos.Contains("cliente")) ClienteCombo.Focus();
        }

        // O destaque some assim que o usuário corrige o campo.
        private void NomeTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(NomeTextBox.Text))
                NomeBorder.ClearValue(System.Windows.Controls.Border.BorderBrushProperty);
        }

        private void ClienteCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ClienteCombo.SelectedItem != null)
                ClienteErroBorder.Visibility = Visibility.Collapsed;
        }

        #endregion

        #region Validações

        private void ValidarEntradaSemNegativo(object sender, TextCompositionEventArgs e)
        {
            if (e.Text == "-")
            {
                MessageBox.Show("Não é permitido valores negativos.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                e.Handled = true;
                return;
            }

            Regex regex = new Regex("[^0-9,]+");
            bool temCaractereInvalido = regex.IsMatch(e.Text);

            if (temCaractereInvalido)
            {
                MessageBox.Show("Este campo aceita apenas números positivos.", "Entrada Inválida", MessageBoxButton.OK, MessageBoxImage.Information);
                e.Handled = true;
            }
        }

        #endregion
    }
}