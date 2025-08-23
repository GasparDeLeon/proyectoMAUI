using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ObligatorioTT.Models
{
    public class ClimaModel
    {
        public string? Fecha { get; set; }
        public string? Descripcion { get; set; }
        public string? Icono { get; set; }
        public decimal? Temperatura { get; set; }

        // NUEVO: para “clima actual”
        public decimal? TempMax { get; set; }    // °C
        public decimal? TempMin { get; set; }    // °C
        public decimal? Viento { get; set; }     // km/h
        public int? Humedad { get; set; }    // %
        public decimal? Lluvia { get; set; }     // mm (última 1h/3h si existe)

        // NUEVO: para “pronóstico”
        public decimal? Tmax { get; set; }       // °C
        public decimal? Tmin { get; set; }       // °C
        public bool TieneMaxMin { get; set; }
    }
}
