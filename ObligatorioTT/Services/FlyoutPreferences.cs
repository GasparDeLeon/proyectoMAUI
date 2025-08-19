using System;
using Microsoft.Maui.Storage; // Preferences

namespace ObligatorioTT.Services
{
    public static class FlyoutPreferences
    {
        // Claves públicas (usadas en toda la app)
        public const string ShowClima = "show_clima";
        public const string ShowCotizaciones = "show_cotizaciones";
        public const string ShowNoticias = "show_noticias";
        public const string ShowPeliculas = "show_peliculas";
        public const string ShowPatrocinadores = "show_patrocinadores";
        public const string ShowMapa = "show_mapa";

        // Evento para notificar cambios (AppShell se suscribe)
        public static event Action? PreferencesChanged;

        // Accesores simples
        public static bool Get(string key, bool defaultValue = true)
            => Preferences.Get(key, defaultValue);

        public static void Set(string key, bool value)
            => Preferences.Set(key, value);

        // Llamalo después de guardar para refrescar el Flyout
        public static void RaiseChanged() => PreferencesChanged?.Invoke();
    }
}
