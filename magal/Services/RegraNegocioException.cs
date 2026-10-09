using System;

namespace magal.Services
{
    /// <summary>
    /// Falha de regra de negócio (ex.: excluir registro com vínculos). A mensagem já é escrita para o
    /// usuário final, em português, e é exibida como aviso, não como erro técnico.
    /// </summary>
    public class RegraNegocioException : Exception
    {
        public RegraNegocioException(string mensagemParaUsuario) : base(mensagemParaUsuario)
        {
        }
    }
}
