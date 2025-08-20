using System;
using SQLite;

namespace ObligatorioTT.Models
{
    [Table("Usuarios")]
    public class Usuario
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [Unique, NotNull, MaxLength(50)]
        public string UserName { get; set; } = string.Empty;

        [NotNull, MaxLength(100)]
        public string Password { get; set; } = string.Empty;

        [NotNull, MaxLength(120)]
        public string NombreCompleto { get; set; } = string.Empty;

        [NotNull, MaxLength(200)]
        public string Direccion { get; set; } = string.Empty;

        [NotNull, MaxLength(30)]
        public string Telefono { get; set; } = string.Empty;

        [NotNull, MaxLength(120)]
        public string Email { get; set; } = string.Empty;

        [NotNull] // ruta local del archivo
        public string FotoPath { get; set; } = string.Empty;
    }
}
