using System;
using System.Security.Cryptography;

namespace magal.Services
{
    /// <summary>
    /// Gera e valida hashes de senha (PBKDF2/SHA256).
    /// Verify aceita transparentemente senhas antigas gravadas em texto puro
    /// (formato legado, sem o prefixo "V1$"), permitindo migrar a base aos poucos.
    /// </summary>
    public static class PasswordHasher
    {
        private const string Prefixo = "V1";
        private const int Iteracoes = 100_000;
        private const int TamanhoSalt = 16;
        private const int TamanhoHash = 32;

        public static string Hash(string senha)
        {
            if (senha == null) throw new ArgumentNullException(nameof(senha));

            byte[] salt = RandomNumberGenerator.GetBytes(TamanhoSalt);
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(senha, salt, Iteracoes, HashAlgorithmName.SHA256, TamanhoHash);

            return $"{Prefixo}${Iteracoes}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
        }

        public static bool Verify(string senhaDigitada, string valorArmazenado)
        {
            if (senhaDigitada == null || valorArmazenado == null) return false;

            if (!EhHashV1(valorArmazenado))
            {
                // Compatibilidade com registros antigos gravados em texto puro.
                return senhaDigitada == valorArmazenado;
            }

            string[] partes = valorArmazenado.Split('$');
            if (partes.Length != 4) return false;

            if (!int.TryParse(partes[1], out int iteracoes)) return false;

            byte[] salt = Convert.FromBase64String(partes[2]);
            byte[] hashEsperado = Convert.FromBase64String(partes[3]);

            byte[] hashCalculado = Rfc2898DeriveBytes.Pbkdf2(senhaDigitada, salt, iteracoes, HashAlgorithmName.SHA256, hashEsperado.Length);

            return CryptographicOperations.FixedTimeEquals(hashCalculado, hashEsperado);
        }

        /// <summary>
        /// Indica se o valor já está no formato de hash (V1) ou se ainda é texto puro legado.
        /// </summary>
        public static bool EhHashV1(string valorArmazenado)
        {
            return valorArmazenado != null && valorArmazenado.StartsWith(Prefixo + "$", StringComparison.Ordinal);
        }
    }
}
