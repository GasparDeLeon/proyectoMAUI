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
<<<<<<< Updated upstream
=======

        // ✅ NUEVO: coordenadas opcionales
        public double? Latitud { get; set; }
        public double? Longitud { get; set; }

        // Propiedad de conveniencia (no se persiste en SQLite)
        [Ignore]
        public bool TieneCoordenadas => Latitud.HasValue && Longitud.HasValue;
>>>>>>> Stashed changes
    }
}
