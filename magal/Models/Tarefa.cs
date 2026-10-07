using magal.Models;
using System;

namespace magal.Models
{
    public class Tarefa : BaseModel
    {
        private int _id_tarefa;
        public int id_tarefa
        {
            get => _id_tarefa;
            set { _id_tarefa = value; OnPropertyChanged(); }
        }

        private int _id_projeto;
        public int id_projeto
        {
            get => _id_projeto;
            set { _id_projeto = value; OnPropertyChanged(); }
        }

        private string _descricao;
        public string descricao
        {
            get => _descricao;
            set { if (_descricao == value) return; _descricao = value; OnPropertyChanged(); }
        }

        private int _id_funcionario;
        public int id_funcionario
        {
            get => _id_funcionario;
            set
            {
                if (_id_funcionario == value) return;
                _id_funcionario = value;
                OnPropertyChanged();
            }
        }

        private bool _responsavelPendente;
        /// <summary>Destaca em vermelho o responsável não selecionado ao tentar salvar.</summary>
        public bool ResponsavelPendente
        {
            get => _responsavelPendente;
            set { if (_responsavelPendente == value) return; _responsavelPendente = value; OnPropertyChanged(); }
        }

        private Funcionario _funcionario;
        public Funcionario Funcionario
        {
            get => _funcionario;
            set
            {
                // Verificação de referência para evitar loops e processamento desnecessário
                if (ReferenceEquals(_funcionario, value)) return;

                _funcionario = value;

                if (_funcionario != null)
                {
                    _id_funcionario = _funcionario.id_funcionario;
                    ResponsavelPendente = false;
                }

                OnPropertyChanged();
                // Notifica a UI que o ID e o Custo Real mudaram
                OnPropertyChanged(nameof(id_funcionario));
                OnPropertyChanged(nameof(custo_real));
            }
        }

        private decimal _horas_estimadas;
        public decimal horas_estimadas
        {
            get => _horas_estimadas;
            set
            {
                if (_horas_estimadas == value) return;
                _horas_estimadas = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(custo_real)); // Recalcula o subtotal na tela
            }
        }

        private decimal _horas_reais;
        public decimal horas_reais
        {
            get => _horas_reais;
            set { if (_horas_reais == value) return; _horas_reais = value; OnPropertyChanged(); }
        }
        
        public decimal custo_real
        {
            get
            {

                if (Funcionario == null) return 0;

                decimal valorHora = Funcionario.custo_hora > 0
                                    ? Funcionario.custo_hora
                                    : (Funcionario.Cargo?.custo_medio_hora ?? 0);

                return horas_estimadas * valorHora;
            }
        }

        private string _status = "Pendente";
        public string status
        {
            get => _status;
            set { if (_status == value) return; _status = value; OnPropertyChanged(); }
        }
    }
}