using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using magal.ViewModels;
using magal.Services;

namespace magal.Views
{
    public partial class UsuariosListaView : UserControl
    {
        private readonly UsuariosViewModel _viewModel;

        public UsuariosListaView()
        {
            InitializeComponent();
            _viewModel = new UsuariosViewModel();
            this.DataContext = _viewModel;
        }

        /// <summary>
        /// Dispara o carregamento assíncrono dos usuários assim que a tela é carregada.
        /// </summary>
        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                await _viewModel.CarregarUsuariosAsync();
            }
            catch (Exception ex)
            {
                TratadorErros.Mostrar(ex, "carregar os usuários");
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
                mainWindow.AbrirHome();
            }
        }

        /// <summary>
        /// Abre o modal de cadastro de novo usuário e recarrega a lista ao fechar.
        /// </summary>
        private async void BtnNovoUsuario_Click(object sender, RoutedEventArgs e)
        {
            CadastrarUsuarioDialog dialog = new CadastrarUsuarioDialog();
            dialog.Owner = Application.Current.Windows.OfType<magal.MainWindow>().FirstOrDefault();

            // Abre como caixa de diálogo modal
            if (dialog.ShowDialog() == true || dialog.DialogResult == null)
            {
                try
                {
                    // Recarrega a lista automaticamente para trazer o usuário recém-criado
                    await _viewModel.CarregarUsuariosAsync();
                }
                catch (Exception ex)
                {
                    TratadorErros.Mostrar(ex, "carregar os usuários");
                }
            }
        }
    }
}