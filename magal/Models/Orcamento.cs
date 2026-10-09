using magal.ViewModels;
using magal.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace magal.Models
{
    public class Orcamento : BaseModel
    {
        private int _id_orcamento;
        public int id_orcamento
        {
            get => _id_orcamento;
            set { _id_orcamento = value; OnPropertyChanged(); }
        }

        private decimal _margem_percentual;
        public decimal margem_percentual
        {
            get => _margem_percentual;
            set
            {
                _margem_percentual = value;
                OnPropertyChanged();
                NotificarMudancasCalculadas();
            }
        }

        private int _validade_dias = 15;
        public int validade_dias
        {
            get => _validade_dias;
            set { _validade_dias = value; OnPropertyChanged(); }
        }

  
        private string _forma_pagamento;
        public string forma_pagamento
        {
            get => _forma_pagamento;
            set { _forma_pagamento = value; OnPropertyChanged(); }
        }

        private DateTime? _prazo_entrega; 
        public DateTime? prazo_entrega   
        {
            get => _prazo_entrega;
            set { _prazo_entrega = value; OnPropertyChanged(); }
        }

        private string _observacoes;
        public string observacoes
        {
            get => _observacoes;
            set { _observacoes = value; OnPropertyChanged(); }
        }
        // =========================================================================

        private decimal _percentual_impostos;
        public decimal percentual_impostos
        {
            get => _percentual_impostos;
            set
            {
                _percentual_impostos = value;
                OnPropertyChanged();
                NotificarMudancasCalculadas();
            }
        }

        private decimal _custo_base;
        public decimal custo_base
        {
            get => _custo_base;
            set
            {
                _custo_base = value;
                OnPropertyChanged();
                NotificarMudancasCalculadas();
            }
        }

        private decimal? _valor_margem_manual;
        public decimal valor_margem
        {
            // Markup: acréscimo percentual sobre o custo (custo 100 e markup 30% => lucro 30).
            get => _valor_margem_manual ?? (custo_base * (margem_percentual / 100));
            set { _valor_margem_manual = value; OnPropertyChanged(); }
        }

        private decimal? _valor_impostos_manual;
        public decimal valor_impostos
        {
            get => _valor_impostos_manual ?? ((custo_base + valor_margem) * (percentual_impostos / 100));
            set { _valor_impostos_manual = value; OnPropertyChanged(); }
        }

        private decimal? _valor_final_manual;
        public decimal valor_final
        {
            get
            {
                // se o custo base for maior que zero, ele tenta calcular o total atualizado
                // se for zero (como na lista de histórico), ele usa o valor salvo no banco
                decimal calculado = custo_base + valor_margem + valor_impostos;
                return (calculado > 0) ? calculado : (_valor_final_manual ?? 0);
            }
            set
            {
                _valor_final_manual = value;
                OnPropertyChanged();
            }
        }

        public DateTime data_criacao { get; set; } = DateTime.Now;

        /// <summary>
        /// Margem de lucro sobre o preço de venda equivalente ao markup digitado: markup / (100 + markup).
        /// Ex.: markup 30% => 23,08%. Somente leitura (exibição).
        /// </summary>
        public decimal MargemSobreVenda => margem_percentual <= -100 ? 0 : margem_percentual / (100 + margem_percentual) * 100;

        private void NotificarMudancasCalculadas()
        {
            _valor_margem_manual = null;
            _valor_impostos_manual = null;
            _valor_final_manual = null;

            OnPropertyChanged(nameof(valor_margem));
            OnPropertyChanged(nameof(MargemSobreVenda));
            OnPropertyChanged(nameof(valor_impostos));
            OnPropertyChanged(nameof(valor_final));
        }

        public void CalcularTotal(List<Tarefa> tarefas, List<Custo> custosExtras)
        {
            decimal totalMaoDeObra = tarefas?.Sum(t => t.custo_real) ?? 0;
            decimal totalCustosExtras = custosExtras?.Sum(c => c.valor) ?? 0;

            // dispara o setter de custo_base, que limpa os manuais e recalcula tudo
            custo_base = totalMaoDeObra + totalCustosExtras;
        }
    }
}