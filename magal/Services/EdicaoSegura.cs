using System.Reflection;

namespace magal.Services
{
    /// <summary>
    /// Os diálogos de edição alteram o objeto que a lista já exibe antes de gravar. Se o banco ou uma regra
    /// de negócio recusar o salvamento, é preciso devolver os valores originais, senão a lista continua
    /// mostrando um dado que nunca foi gravado.
    /// </summary>
    public static class EdicaoSegura
    {
        /// <summary>Cópia rasa das propriedades públicas graváveis, tirada antes de alterar o objeto.</summary>
        public static T Copiar<T>(T origem) where T : class, new()
        {
            var copia = new T();
            Transferir(origem, copia);
            return copia;
        }

        /// <summary>Devolve ao objeto os valores da cópia tirada por <see cref="Copiar{T}"/>.</summary>
        public static void Restaurar<T>(T destino, T copia) where T : class
        {
            if (destino == null || copia == null) return;
            Transferir(copia, destino);
        }

        private static void Transferir<T>(T de, T para) where T : class
        {
            foreach (var p in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (p.CanRead && p.CanWrite && p.GetIndexParameters().Length == 0)
                    p.SetValue(para, p.GetValue(de));
            }
        }
    }
}
