using System.ComponentModel;
using System.Windows.Controls;
using LiveCharts;
using LiveCharts.Wpf;

namespace magal.Views
{
    /// <summary>
    /// Tooltip customizado para os gráficos de pizza (Status/Tipo) da tela de gráficos.
    /// Mostra o valor bruto de cada ponto no hover, independente do LabelPoint usado
    /// para exibir a porcentagem nas fatias.
    /// </summary>
    public partial class ContagemTooltip : UserControl, IChartTooltip, INotifyPropertyChanged
    {
        private TooltipData _data;

        public TooltipData Data
        {
            get => _data;
            set
            {
                _data = value;
                OnPropertyChanged(nameof(Data));
            }
        }

        public TooltipSelectionMode? SelectionMode { get; set; }

        public ContagemTooltip()
        {
            InitializeComponent();
            DataContext = this;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
