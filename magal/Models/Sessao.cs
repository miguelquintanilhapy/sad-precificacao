namespace magal
{
    public static class Sessao
    {
        public static magal.Models.Usuario UsuarioLogado { get; set; }

        /// <summary>Indica se o usuário logado é Administrador.</summary>
        public static bool EhAdministrador => UsuarioLogado != null && UsuarioLogado.nivel == "Administrador";

        /// <summary>
        /// Visibilidade para controles restritos ao Administrador (usada via x:Static no XAML):
        /// quem não tem permissão nem enxerga o controle.
        /// </summary>
        public static System.Windows.Visibility VisibilidadeAdmin =>
            EhAdministrador ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;

        /// <summary>Inverso de <see cref="VisibilidadeAdmin"/>: controles exibidos apenas para quem não é Administrador.</summary>
        public static System.Windows.Visibility VisibilidadeNaoAdmin =>
            EhAdministrador ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;
    }
}