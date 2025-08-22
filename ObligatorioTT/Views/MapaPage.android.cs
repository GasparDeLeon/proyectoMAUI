#if ANDROID
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Maui.Devices.Sensors; // Location
using ObligatorioTT.Models;
using ObligatorioTT.Services;

// Alias para Maps (usamos el control de MAUI)
using ControlsMaps = Microsoft.Maui.Controls.Maps;
using MauiMaps = Microsoft.Maui.Maps;

namespace ObligatorioTT.Views
{
    /// <summary>
    /// Helper específico de Android para cargar pines de sponsors en un Map ya existente.
    /// </summary>
    internal static class MapaPageAndroidHelpers
    {
        /// <summary>
        /// Carga pines de sponsors en el mapa. Si adjustCamera=true, además encuadra para verlos a todos.
        /// </summary>
        public static async Task LoadSponsorPinsAsync(ControlsMaps.Map map, bool adjustCamera = false)
        {
            if (map == null) return;

            await SponsorRepository.Inst.InitAsync();
            var sponsors = await SponsorRepository.Inst.GetAllAsync();

            // Limpiar pines existentes de sponsors (dejamos el pin del usuario si existe)
            // Tip: si querés diferenciar, podrías usar Pin.Type o Label prefix para filtrar.
            var toRemove = map.Pins.Where(p => p != null && (p.Label != "Estás aquí")).ToList();
            foreach (var p in toRemove)
                map.Pins.Remove(p);

            var coords = new List<Location>();

            foreach (var s in sponsors)
            {
                if (s.Latitud is double lat && s.Longitud is double lng)
                {
                    var loc = new Location(lat, lng);
                    coords.Add(loc);

                    var pin = new ControlsMaps.Pin
                    {
                        Label = s.Nombre,
                        Address = s.Direccion,
                        Location = loc,
                        Type = ControlsMaps.PinType.Place
                    };

                    map.Pins.Add(pin);
                }
            }

            if (adjustCamera && coords.Count > 0)
            {
                double minLat = coords.Min(l => l.Latitude);
                double maxLat = coords.Max(l => l.Latitude);
                double minLng = coords.Min(l => l.Longitude);
                double maxLng = coords.Max(l => l.Longitude);

                var center = new Location(
                    (minLat + maxLat) / 2.0,
                    (minLng + maxLng) / 2.0
                );

                // Radio aprox en metros (con margen)
                double latMeters = 111_000 * Math.Max(0.000001, (maxLat - minLat));
                double lngMeters = 111_000 * Math.Cos(center.Latitude * Math.PI / 180.0) * Math.Max(0.000001, (maxLng - minLng));
                double radiusMeters = Math.Max(latMeters, lngMeters) / 2.0 + 500;

                map.MoveToRegion(MauiMaps.MapSpan.FromCenterAndRadius(center, MauiMaps.Distance.FromMeters(Math.Max(500, radiusMeters))));
            }
        }
    }
}
#endif
