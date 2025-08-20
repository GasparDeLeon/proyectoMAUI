using SQLite;

namespace ObligatorioTT.Models
{
    public class Sponsor
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [MaxLength(100), NotNull]   // requerido
        public string Nombre { get; set; } = string.Empty;

        [MaxLength(200), NotNull]   // requerido
        public string Direccion { get; set; } = string.Empty;

        [NotNull]                   // requerido (ruta local de la imagen)
        public string LogoPath { get; set; } = string.Empty;

        // Coordenadas opcionales (se completan en Android)
        public double? Latitud { get; set; }
        public double? Longitud { get; set; }

        // Conveniencia (no se persiste)
        [Ignore]
        public bool TieneCoordenadas => Latitud.HasValue && Longitud.HasValue;
    }
}
