using System;
using System.Collections.Generic;

namespace magal.Services
{
    /// <summary>
    /// Controle de tentativas de login em memória: bloqueia temporariamente um e-mail
    /// após várias falhas seguidas, para dificultar força bruta contra a tela de login.
    /// O estado é por execução do processo (não persiste em banco).
    /// </summary>
    public static class LoginThrottle
    {
        private const int MaxTentativas = 5;
        private static readonly TimeSpan DuracaoBloqueio = TimeSpan.FromMinutes(1);

        private class Registro
        {
            public int Tentativas;
            public DateTime? BloqueadoAte;
        }

        private static readonly Dictionary<string, Registro> _registros =
            new Dictionary<string, Registro>(StringComparer.OrdinalIgnoreCase);

        private static readonly object _lock = new object();

        public static bool EstaBloqueado(string email, out TimeSpan tempoRestante)
        {
            lock (_lock)
            {
                tempoRestante = TimeSpan.Zero;

                if (!_registros.TryGetValue(email, out var registro) || !registro.BloqueadoAte.HasValue)
                    return false;

                var restante = registro.BloqueadoAte.Value - DateTime.Now;
                if (restante <= TimeSpan.Zero)
                {
                    _registros.Remove(email);
                    return false;
                }

                tempoRestante = restante;
                return true;
            }
        }

        public static void RegistrarFalha(string email)
        {
            lock (_lock)
            {
                if (!_registros.TryGetValue(email, out var registro))
                {
                    registro = new Registro();
                    _registros[email] = registro;
                }

                registro.Tentativas++;

                if (registro.Tentativas >= MaxTentativas)
                {
                    registro.BloqueadoAte = DateTime.Now.Add(DuracaoBloqueio);
                }
            }
        }

        public static void RegistrarSucesso(string email)
        {
            lock (_lock)
            {
                _registros.Remove(email);
            }
        }
    }
}
