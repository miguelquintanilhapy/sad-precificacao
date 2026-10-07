using magal.Models;
using magal.ViewModels;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
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
            if (campos.Contains("validade")) ValidadeBorder.BorderBrush = BrushErro;

            if (campos.Contains("nome")) NomeTextBox.Focus();
            else if (campos.Contains("cliente")) ClienteCombo.Focus();
            else if (campos.Contains("validade")) ValidadeTextBox.Focus();
        }

        // O destaque some assim que o usuário corrige o campo.
        private void NomeTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(NomeTextBox.Text))
                NomeBorder.ClearValue(System.Windows.Controls.Border.BorderBrushProperty);
        }

        private void ValidadeTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Campo apagado não converte para int e o binding manteria o valor anterior; avisa a ViewModel.
            if (ViewModel != null) ViewModel.ValidadeEmBranco = string.IsNullOrWhiteSpace(ValidadeTextBox.Text);

            if (int.TryParse(ValidadeTextBox.Text, out int dias) && dias > 0)
                ValidadeBorder.ClearValue(System.Windows.Controls.Border.BorderBrushProperty);
        }

        private void ClienteCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ClienteCombo.SelectedItem != null)
                ClienteErroBorder.Visibility = Visibility.Collapsed;
        }

        #endregion

        #region Pesquisa de responsável

        // Cada ComboBox de responsável recebe sua própria view para filtrar sem afetar as outras tarefas.
        private void ResponsavelCombo_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is ComboBox combo && ViewModel != null)
                ConfigurarPesquisa(combo, ViewModel.Funcionarios, o => (o as Funcionario)?.nome);
        }

        private void ClienteCombo_Loaded(object sender, RoutedEventArgs e)
        {
            if (ViewModel != null)
                ConfigurarPesquisa(ClienteCombo, ViewModel.Clientes, o => (o as Cliente)?.nome);
        }

        /// <summary>Transforma o ComboBox editável em campo de pesquisa que filtra a lista pelo texto digitado.</summary>
        private static void ConfigurarPesquisa(ComboBox combo, System.Collections.IList origem, System.Func<object, string> nomeDe)
        {
            if (combo.Tag is ListCollectionView) return;

            var view = new ListCollectionView(origem);
            combo.Tag = view;
            combo.ItemsSource = view;

            // Dentro de DataTemplate os templates internos só existem depois do primeiro layout.
            ManterCorDeFundo(combo);
            combo.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Loaded,
                new System.Action(() => ManterCorDeFundo(combo)));

            bool filtrando = false;

            combo.AddHandler(System.Windows.Controls.Primitives.TextBoxBase.TextChangedEvent,
                new TextChangedEventHandler((s, args) =>
                {
                    if (filtrando) return;

                    string texto = combo.Text ?? string.Empty;
                    if (combo.SelectedItem != null && nomeDe(combo.SelectedItem) == texto)
                    {
                        view.Filter = null;
                        return;
                    }

                    filtrando = true;
                    var caixa = combo.Template.FindName("PART_EditableTextBox", combo) as TextBox;
                    int caret = caixa?.CaretIndex ?? texto.Length;

                    view.Filter = string.IsNullOrWhiteSpace(texto)
                        ? null
                        : o => ContemSemAcento(nomeDe(o), texto);

                    // Remover o item selecionado da view pode reescrever o texto; restaura o que foi digitado.
                    if (combo.Text != texto) combo.Text = texto;
                    if (caixa != null) { caixa.CaretIndex = caret; caixa.SelectionLength = 0; }

                    // Nome digitado por completo já seleciona o item, sem precisar clicar na lista.
                    var exato = BuscarExato(origem, nomeDe, texto);
                    if (exato != null)
                    {
                        combo.SelectedItem = exato;
                        if (caixa != null) { caixa.CaretIndex = combo.Text.Length; caixa.SelectionLength = 0; }
                    }

                    // Sem resultados, a lista fica fechada em vez de mostrar um painel vazio.
                    bool abrir = combo.IsKeyboardFocusWithin && !view.IsEmpty;
                    if (abrir && !combo.IsDropDownOpen && caixa != null)
                    {
                        // Abrir a lista seleciona todo o texto, e a próxima tecla apagaria o que já foi digitado.
                        int pos = caixa.CaretIndex;
                        combo.IsDropDownOpen = true;
                        combo.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Input,
                            new System.Action(() => { caixa.SelectionLength = 0; caixa.CaretIndex = pos; }));
                    }
                    else
                    {
                        combo.IsDropDownOpen = abrir;
                    }
                    filtrando = false;
                }));

            // Ao sair do campo, descarta texto que não corresponde a nenhum item.
            combo.LostKeyboardFocus += (s, args) =>
            {
                if (combo.IsKeyboardFocusWithin) return;
                filtrando = true;
                view.Filter = null;
                var exato = BuscarExato(origem, nomeDe, combo.Text);
                if (exato != null) combo.SelectedItem = exato;
                combo.Text = combo.SelectedItem != null ? nomeDe(combo.SelectedItem) : string.Empty;
                filtrando = false;
            };
        }

        // Mesmo gradiente cinza que o tema padrão usa nos ComboBoxes não editáveis (ignora a propriedade Background).
        private static readonly System.Windows.Media.Brush FundoPadraoCombo = new System.Windows.Media.LinearGradientBrush(
            System.Windows.Media.Color.FromRgb(0xF0, 0xF0, 0xF0),
            System.Windows.Media.Color.FromRgb(0xE5, 0xE5, 0xE5), 90);

        // O template editável do WPF pinta o campo de branco; reaplica o fundo usado pelos demais ComboBoxes.
        private static void ManterCorDeFundo(ComboBox combo)
        {
            combo.ApplyTemplate();
            if (combo.Template.FindName("PART_EditableTextBox", combo) is TextBox caixa)
            {
                caixa.Background = System.Windows.Media.Brushes.Transparent;
                caixa.Padding = new Thickness(6, 0, 0, 0);
            }

            void Pintar(DependencyObject pai)
            {
                for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(pai); i++)
                {
                    var filho = System.Windows.Media.VisualTreeHelper.GetChild(pai, i);
                    if (filho is Border b && b.Background is System.Windows.Media.SolidColorBrush sb
                        && sb.Color == System.Windows.Media.Colors.White)
                        b.Background = FundoPadraoCombo;
                    Pintar(filho);
                }
            }
            Pintar(combo);
        }

        private static object BuscarExato(System.Collections.IList origem, System.Func<object, string> nomeDe, string texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return null;
            var cmp = System.Globalization.CultureInfo.GetCultureInfo("pt-BR").CompareInfo;
            var opcoes = System.Globalization.CompareOptions.IgnoreCase | System.Globalization.CompareOptions.IgnoreNonSpace;
            var iguais = origem.Cast<object>().Where(o => cmp.Compare(nomeDe(o)?.Trim(), texto.Trim(), opcoes) == 0).ToList();
            return iguais.Count == 1 ? iguais[0] : null;
        }

        private static bool ContemSemAcento(string origem, string busca)
        {
            if (string.IsNullOrEmpty(origem)) return false;
            var cmp = System.Globalization.CultureInfo.GetCultureInfo("pt-BR").CompareInfo;
            return cmp.IndexOf(origem, busca.Trim(),
                System.Globalization.CompareOptions.IgnoreCase | System.Globalization.CompareOptions.IgnoreNonSpace) >= 0;
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