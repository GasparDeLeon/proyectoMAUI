#if ANDROID
using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Maui.Controls;
using ObligatorioTT.Models;
using ObligatorioTT.Services;

// Alias para separar namespaces de Maps
using ControlsMaps = Microsoft.Maui.Controls.Maps;
using MauiMaps = Microsoft.Maui.Maps;

using Microsoft.Maui.Devices.Sensors; // Location

namespace ObligatorioTT.Views
{
    // Esta clase complementa tu MapaPage existente SOLO en Android
    public partial class MapaPage : ContentPage
    {
        private ControlsMaps.Map _map;

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            await SponsorRepository.Inst.InitAsync();
            var sponsors = await SponsorRepository.Inst.GetAllAsync();

            // Mapa a pantalla completa
            _map = new ControlsMaps.Map
            {
                MapType = MauiMaps.MapType.Street,
                IsShowingUser = false,
                VerticalOptions = LayoutOptions.FillAndExpand
            };

            // Limpia pines y agrega los de sponsors con coordenadas
            _map.Pins.Clear();
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

                    _map.Pins.Add(pin);
                }
            }

            // Encadre para ver todos los pines
            if (coords.Count > 0)
            {
                double minLat = coords.Min(l => l.Latitude);
                double maxLat = coords.Max(l => l.Latitude);
                double minLng = coords.Min(l => l.Longitude);
                double maxLng = coords.Max(l => l.Longitude);

                var center = new Location(
                    (minLat + maxLat) / 2.0,
                    (minLng + maxLng) / 2.0
                );

                // Calcular radio aprox en metros (con pequeño margen)
                double latMeters = 111_000 * Math.Max(0.000001, (maxLat - minLat));
                double lngMeters = 111_000 * Math.Cos(center.Latitude * Math.PI / 180.0) * Math.Max(0.000001, (maxLng - minLng));
                double radiusMeters = Math.Max(latMeters, lngMeters) / 2.0 + 500;

                _map.MoveToRegion(MauiMaps.MapSpan.FromCenterAndRadius(center, MauiMaps.Distance.FromMeters(Math.Max(500, radiusMeters))));
            }
            else
            {
                // Fallback (Maldonado aprox)
                var center = new Location(-34.9, -54.95);
                _map.MoveToRegion(MauiMaps.MapSpan.FromCenterAndRadius(center, MauiMaps.Distance.FromKilometers(5)));
            }

            // Reemplaza el contenido de la página por el mapa (solo Android)
            Content = _map;
        }
    }
}
#endif
