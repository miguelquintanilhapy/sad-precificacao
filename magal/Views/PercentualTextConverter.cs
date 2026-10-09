using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace magal.Views
{
    /// <summary>
    /// Permite digitar decimais num campo ligado a um decimal com UpdateSourceTrigger=PropertyChanged.
    /// Sem isso, "5," vira "5" assim que o valor é gravado e a vírgula nunca chega a ser digitada.
    /// Use uma instância por campo: ela guarda o último texto digitado.
    /// </summary>
    public class PercentualTextConverter : MarkupExtension, IValueConverter
    {
        private static readonly CultureInfo PtBR = new CultureInfo("pt-BR");
        private string _ultimoTexto;

        // Cada uso no XAML ({local:PercentualTextConverter}) gera uma instância própria.
        public override object ProvideValue(IServiceProvider serviceProvider) => new PercentualTextConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            decimal v = value is decimal d ? d : 0;

            // Se o texto digitado já representa este valor ("5," ou "5,50"), não reescreve.
            if (_ultimoTexto != null && decimal.TryParse(_ultimoTexto, NumberStyles.Number, PtBR, out var digitado) && digitado == v)
                return _ultimoTexto;

            return v.ToString("0.##", PtBR);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Colar "12.5" também vale: em pt-BR o ponto seria lido como milhar (125).
            _ultimoTexto = (value as string)?.Replace('.', ',');
            if (string.IsNullOrWhiteSpace(_ultimoTexto)) return 0m;

            return decimal.TryParse(_ultimoTexto, NumberStyles.Number, PtBR, out var v) ? v : Binding.DoNothing;
        }
    }
}
