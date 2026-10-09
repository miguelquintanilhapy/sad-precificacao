using System;
using System.Windows;
using System.Linq;
using System.Threading.Tasks;
using magal.Models;
using magal.Services;
using magal.Data.Repositories;

namespace magal.ViewModels
{
    public class GerenciamentoViewModel : BaseModel
    {
        #region Atributos e Campos Privados

        private readonly FuncionarioRepository _funcionarioRepo;
        private readonly ClienteRepository _clienteRepo;
        private readonly CargoRepository _cargoRepo;
        private readonly CatalogoCustoRepository _catalogoCustoRepo;

        private string _totalFuncionarios;
        private string _totalClientes;
        private string _totalCargos;
        private string _totalCustos;
        private bool _isLoading = true;

        #endregion

        #region Propriedades do Sistema

        /// <summary>
        /// Sinaliza se a View está buscando informações assíncronas do banco de dados.
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }

        public string TotalFuncionarios
        {
            get => _totalFuncionarios;
            set { _totalFuncionarios = value; OnPropertyChanged(); }
        }

        public string TotalClientes
        {
            get => _totalClientes;
            set { _totalClientes = value; OnPropertyChanged(); }
        }

        public string TotalCargos
        {
            get => _totalCargos;
            set { _totalCargos = value; OnPropertyChanged(); }
        }

        public string TotalCustos
        {
            get => _totalCustos;
            set { _totalCustos = value; OnPropertyChanged(); }
        }

        #endregion

        #region Construtores

        public GerenciamentoViewModel()
        {
            _funcionarioRepo = new FuncionarioRepository();
            _clienteRepo = new ClienteRepository();
            _cargoRepo = new CargoRepository();
            _catalogoCustoRepo = new CatalogoCustoRepository();
        }

        #endregion

        #region Métodos Públicos

        /// <summary>
        /// Consulta assincronamente os repositórios para preencher os indicadores na interface.
        /// </summary>
        public async Task CarregarIndicadores()
        {
            try
            {
                IsLoading = true;

                var funcionarios = await _funcionarioRepo.ListarTodos();
                TotalFuncionarios = (funcionarios?.Count ?? 0).ToString("D2");

                var clientes = await _clienteRepo.ListarTodos();
                TotalClientes = (clientes?.Count ?? 0).ToString("D2");

                var cargos = await _cargoRepo.ListarTodos();
                TotalCargos = (cargos?.Count ?? 0).ToString("D2");

                var custos = await _catalogoCustoRepo.ListarTodos();
                TotalCustos = (custos?.Count ?? 0).ToString("D2");

                await Task.Delay(100); // Garante a estabilização visual da renderização do WPF
            }
            catch (Exception ex)
            {
                TotalFuncionarios = "00";
                TotalClientes = "00";
                TotalCargos = "00";
                TotalCustos = "00";

                TratadorErros.Registrar(ex, "Falha ao carregar os totais do painel de gerenciamento");
            }
            finally
            {
                IsLoading = false;
            }
        }

        #endregion
    }
}