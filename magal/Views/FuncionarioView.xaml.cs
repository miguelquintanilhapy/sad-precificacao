using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using magal.ViewModels;
using magal.Services;

namespace magal.Views
{
    public partial class FuncionarioView : UserControl
    {
        private readonly FuncionarioViewModel _viewModel;

        public FuncionarioView()
        {
            InitializeComponent();
            _viewModel = new FuncionarioViewModel();
            this.DataContext = _viewModel;
        }

        /// <summary>
        /// Dispara o carregamento assíncrono dos funcionários assim que a tela é carregada.
        /// </summary>
        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                await _viewModel.CarregarFuncionarios();
            }
            catch (Exception ex)
            {
                TratadorErros.Mostrar(ex, "carregar os funcionários");
            }
        }

        /// <summary>
        /// Trata o evento de clique do botão voltar.
        /// </summary>
        private void BtnVoltar_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = Application.Current.Windows.OfType<magal.MainWindow>().FirstOrDefault();

            if (mainWindow != null)
            {
                mainWindow.AbrirGerenciamento();
            }
        }
    }
}