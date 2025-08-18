using Microsoft.Maui.Storage;
using System;

namespace ObligatorioTT.Services
{
    public static class FlyoutPreferences
    {
        // Claves (una por sección)
        public const string ShowInicio = "show_inicio";
        public const string ShowClima = "show_clima";
        public const string ShowCotizaciones = "show_cotizaciones";
        public const string ShowNoticias = "show_noticias";
        public const string ShowPeliculas = "show_peliculas";
        public const string ShowPatrocinadores = "show_patrocinadores";
        public const string ShowMapa = "show_mapa"; // si tenés mapa
        // Agregá más si tuvieras otras secciones

        // Valores por defecto (podés ajustarlos)
        public static bool Get(string key, bool defaultValue = true)
            => Preferences.Default.Get(key, defaultValue);

        public static void Set(string key, bool value)
            => Preferences.Default.Set(key, value);

        // Evento simple para notificar cambios
        public static event Action? PreferencesChanged;
        public static void RaiseChanged() => PreferencesChanged?.Invoke();
    }
}
