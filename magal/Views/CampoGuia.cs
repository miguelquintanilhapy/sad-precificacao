using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace magal.Views
{
    /// <summary>
    /// Texto-guia ("Ex.: ...") e destaque de erro (contorno vermelho) para TextBox, PasswordBox e ComboBox,
    /// desenhados por cima do campo (Adorner): funciona com qualquer estilo/template sem alterá-lo.
    ///
    /// Uso no XAML:  g:CampoGuia.Texto="Ex.: Maria Silva"
    /// Uso no código: CampoGuia.MarcarPendentes(TxtNome, ComboCargo);  // vermelho + foco no primeiro vazio
    ///                CampoGuia.MarcarInvalidos(TxtEmail);              // vermelho + foco, mesmo preenchido
    /// O destaque some sozinho assim que o usuário altera o campo.
    /// </summary>
    public static class CampoGuia
    {
        private static readonly Brush BrushDica = Congelar(new SolidColorBrush(Color.FromRgb(0x94, 0xA3, 0xB8)));
        private static readonly Brush BrushErro = Congelar(new SolidColorBrush(Color.FromRgb(0xDC, 0x26, 0x26)));

        private static readonly ConditionalWeakTable<Control, GuiaAdorner> Adorners = new();
        private static readonly ConditionalWeakTable<Control, object> Ligados = new();

        #region Propriedades anexadas

        public static readonly DependencyProperty TextoProperty =
            DependencyProperty.RegisterAttached("Texto", typeof(string), typeof(CampoGuia),
                new PropertyMetadata(null, AoMudar));

        public static readonly DependencyProperty ErroProperty =
            DependencyProperty.RegisterAttached("Erro", typeof(bool), typeof(CampoGuia),
                new PropertyMetadata(false, AoMudar));

        public static string GetTexto(DependencyObject d) => (string)d.GetValue(TextoProperty);
        public static void SetTexto(DependencyObject d, string v) => d.SetValue(TextoProperty, v);
        public static bool GetErro(DependencyObject d) => (bool)d.GetValue(ErroProperty);
        public static void SetErro(DependencyObject d, bool v) => d.SetValue(ErroProperty, v);

        #endregion

        #region API de validação

        /// <summary>Marca em vermelho os campos vazios e leva o cursor ao primeiro. Retorna true se havia algum.</summary>
        public static bool MarcarPendentes(params Control[] campos)
        {
            var vazios = campos.Where(EstaVazio).ToList();
            MarcarInvalidos(vazios.ToArray());
            return vazios.Count > 0;
        }

        /// <summary>Marca em vermelho os campos informados (ex.: formato inválido) e leva o cursor ao primeiro.</summary>
        public static void MarcarInvalidos(params Control[] campos)
        {
            foreach (var c in campos) SetErro(c, true);
            campos.FirstOrDefault()?.Focus();
        }

        #endregion

        #region Internos

        private static Brush Congelar(Brush b) { b.Freeze(); return b; }

        internal static bool EstaVazio(Control c) => c switch
        {
            TextBox t => string.IsNullOrWhiteSpace(t.Text),
            PasswordBox p => string.IsNullOrEmpty(p.Password),
            ComboBox cb => cb.SelectedItem == null && string.IsNullOrWhiteSpace(cb.Text),
            _ => false
        };

        private static void AoMudar(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not Control c) return;

            Ligar(c);

            if (c.IsLoaded) Atualizar(c);
        }

        // Liga uma única vez os eventos que redesenham o campo e limpam o erro quando o usuário o altera.
        private static void Ligar(Control c)
        {
            if (Ligados.TryGetValue(c, out _)) return;
            Ligados.Add(c, new object());

            c.Loaded += (_, _) => Atualizar(c);

            switch (c)
            {
                case TextBox t: t.TextChanged += (_, _) => AoEditar(c); break;
                case PasswordBox p: p.PasswordChanged += (_, _) => AoEditar(c); break;
                case ComboBox cb: cb.SelectionChanged += (_, _) => AoEditar(c); break;
            }
        }

        private static void AoEditar(Control c)
        {
            if (GetErro(c)) SetErro(c, false);
            else Atualizar(c);
        }

        private static void Atualizar(Control c)
        {
            if (!Adorners.TryGetValue(c, out var adorner))
            {
                var camada = AdornerLayer.GetAdornerLayer(c);
                if (camada == null) return;

                adorner = new GuiaAdorner(c);
                camada.Add(adorner);
                Adorners.Add(c, adorner);
            }

            adorner.InvalidateVisual();
        }

        /// <summary>Posição X onde o texto digitado começa dentro do campo, para o texto-guia ficar no mesmo ponto.</summary>
        private static double InicioDoTexto(Control c)
        {
            c.ApplyTemplate();
            var nome = c is ComboBox ? "ContentSite" : "PART_ContentHost";

            if (c.Template?.FindName(nome, c) is FrameworkElement host && host.IsLoaded)
            {
                var x = host.TransformToAncestor(c).Transform(new Point(0, 0)).X;
                return x + (c is ComboBox ? 1 : 2);
            }

            return c.BorderThickness.Left + c.Padding.Left + 2;
        }

        private sealed class GuiaAdorner : Adorner
        {
            public GuiaAdorner(Control campo) : base(campo) => IsHitTestVisible = false;

            protected override void OnRender(DrawingContext dc)
            {
                var c = (Control)AdornedElement;
                double w = c.ActualWidth, h = c.ActualHeight;
                if (w <= 0 || h <= 0) return;

                if (GetErro(c))
                    dc.DrawRectangle(null, new Pen(BrushErro, 1.5), new Rect(0.75, 0.75, w - 1.5, h - 1.5));

                var texto = GetTexto(c);
                if (string.IsNullOrEmpty(texto) || !EstaVazio(c)) return;

                double esquerda = InicioDoTexto(c);
                double reservado = c is ComboBox ? 28 : 8;

                var ft = new FormattedText(
                    texto,
                    CultureInfo.CurrentUICulture,
                    c.FlowDirection,
                    new Typeface(c.FontFamily, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal),
                    c.FontSize,
                    BrushDica,
                    VisualTreeHelper.GetDpi(this).PixelsPerDip)
                {
                    MaxTextWidth = System.Math.Max(1, w - esquerda - reservado),
                    MaxLineCount = 1,
                    Trimming = TextTrimming.CharacterEllipsis
                };

                dc.DrawText(ft, new Point(esquerda, (h - ft.Height) / 2));
            }
        }

        #endregion
    }
}
