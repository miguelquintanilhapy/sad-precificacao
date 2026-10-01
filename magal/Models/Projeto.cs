using System;
using System.Collections.ObjectModel;

namespace magal.Models
{
    public class Projeto : BaseModel
    {
        public int id_projeto { get; set; }
        public int id_usuario { get; set; }
        public int id_cliente { get; set; }

        // Notificam a mudança para que o texto-guia dos campos (nome/cliente) acompanhe o que o usuário digita ou seleciona.
        private Cliente _cliente;
        public Cliente Cliente
        {
            get => _cliente;
            set { if (_cliente == value) return; _cliente = value; OnPropertyChanged(); }
        }

        private string _nome;
        public string nome
        {
            get => _nome;
            set { if (_nome == value) return; _nome = value; OnPropertyChanged(); }
        }

        public string tipo { get; set; }   // "Produto/Serviço"
        public string status { get; set; } // "Rascunho/Orçado/Aprovado/Executando/Concluído"
        public DateTime data_criacao { get; set; } = DateTime.Now;
        public DateTime? data_conclusao_prevista { get; set; }
        public DateTime DataExpiracao => data_criacao.AddDays(Orcamento?.validade_dias ?? 0);
        public bool EstaVencido => DataExpiracao.Date < DateTime.Today;

        // Objetos de navegação e coleções
        public Orcamento Orcamento { get; set; } = new Orcamento();
        public ObservableCollection<Tarefa> Tarefas { get; set; } = new ObservableCollection<Tarefa>();
        public ObservableCollection<Custo> Custos { get; set; } = new ObservableCollection<Custo>();
    }
}