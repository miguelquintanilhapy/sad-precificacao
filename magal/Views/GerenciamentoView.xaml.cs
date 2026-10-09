using System;
using System.Windows;
using System.Windows.Controls;
using magal.ViewModels;
using magal.Services;

namespace magal.Views
{
    public partial class GerenciamentoView : UserControl
    {
        private readonly GerenciamentoViewModel _viewModel;

        public GerenciamentoView()
        {
            InitializeComponent();

            _viewModel = new GerenciamentoViewModel();
            this.DataContext = _viewModel;
        }

        // ATUALIZADO: Executa a tarefa em segundo plano de forma assíncrona limpa
        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                await _viewModel.CarregarIndicadores();
            }
            catch (Exception ex)
            {
                TratadorErros.Mostrar(ex, "carregar o painel");
            }
        }

        private void BtnFuncionarios_Click(object sender, RoutedEventArgs e)
        {
            var janelaPrincipal = Window.GetWindow(this) as MainWindow;
            if (janelaPrincipal != null)
            {
                janelaPrincipal.MainContentControl.Content = new FuncionarioView();
            }
        }

        private void BtnCargos_Click(object sender, RoutedEventArgs e)
        {
            var janelaPrincipal = Window.GetWindow(this) as MainWindow;
            if (janelaPrincipal != null)
            {
                janelaPrincipal.MainContentControl.Content = new CargoView();
            }
        }

        private void BtnClientes_Click(object sender, RoutedEventArgs e)
        {
            var janelaPrincipal = Window.GetWindow(this) as MainWindow;
            if (janelaPrincipal != null)
            {
                janelaPrincipal.MainContentControl.Content = new ClienteView();
            }
        }

        private void BtnCustos_Click(object sender, RoutedEventArgs e)
        {
            var janelaPrincipal = Window.GetWindow(this) as MainWindow;
            if (janelaPrincipal != null)
            {
                janelaPrincipal.MainContentControl.Content = new CustoView();
            }
        }
    }
}