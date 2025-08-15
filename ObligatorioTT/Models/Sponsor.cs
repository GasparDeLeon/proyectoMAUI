using SQLite;

namespace ObligatorioTT.Models
{
    public class Sponsor
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [MaxLength(100), NotNull]   // requerido
        public string Nombre { get; set; }

        [MaxLength(200), NotNull]   // requerido
        public string Direccion { get; set; }

        [NotNull]                   // requerido (ruta local de la imagen)
        public string LogoPath { get; set; }
    }
}
