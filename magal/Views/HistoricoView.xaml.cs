using System.Windows;
using System.Windows.Controls;
using magal.ViewModels;
using magal.Services;

namespace magal.Views
{
    public partial class HistoricoView : UserControl
    {
        public HistoricoView()
        {
            InitializeComponent();

            var viewModel = new HistoricoViewModel();
            this.DataContext = viewModel;

            viewModel.PropertyChanged += ViewModel_PropertyChanged;

            if (LoadingOverlay != null) LoadingOverlay.Visibility = Visibility.Visible;
            if (meuDataGrid != null) meuDataGrid.IsEnabled = false;
            Loaded += HistoricoView_Loaded;
        }

        private async void HistoricoView_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (this.DataContext is HistoricoViewModel vm)
                {
                    await vm.CarregarHistorico();
                }
            }
            catch (Exception ex)
            {
                TratadorErros.Mostrar(ex, "carregar o histórico");
            }
        }

        private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(HistoricoViewModel.IsLoading) && sender is HistoricoViewModel vm)
            {
                Dispatcher.Invoke(() =>
                {
                    if (vm.IsLoading)
                    {
                        if (LoadingOverlay != null) LoadingOverlay.Visibility = Visibility.Visible;
                        if (meuDataGrid != null) meuDataGrid.IsEnabled = false;
                    }
                    else
                    {
                        if (LoadingOverlay != null) LoadingOverlay.Visibility = Visibility.Collapsed;
                        if (meuDataGrid != null) meuDataGrid.IsEnabled = true;
                    }
                });
            }
        }

        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }
    }
}