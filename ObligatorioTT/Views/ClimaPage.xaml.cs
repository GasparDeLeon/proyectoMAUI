using Microsoft.Maui.Controls;
using ObligatorioTT.Services;
using System;

namespace ObligatorioTT.Views
{
    public partial class ClimaPage : ContentPage
    {
        private bool _isBusy;

        public ClimaPage()
        {
            InitializeComponent();
            _ = CargarClimaAsync();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            // Si querés recargar al entrar:
            // _ = CargarClimaAsync();
        }

        private void SetBusy(bool on)
        {
            _isBusy = on;
            busyOverlay.IsVisible = on;
        }

        // --- Normaliza el icono a URL HTTPS válida (acepta "10d" o http://...) ---
        private static string ToIconUrl(string? iconOrUrl)
        {
            if (string.IsNullOrWhiteSpace(iconOrUrl)) return "";

            // ¿Ya es URL?
            if (Uri.TryCreate(iconOrUrl, UriKind.Absolute, out var u))
            {
                // Forzar https si vino con http
                if (u.Scheme.Equals("http", StringComparison.OrdinalIgnoreCase))
                    return "https" + iconOrUrl.Substring(4);
                return iconOrUrl;
            }

            // Si vino solo el código (p.ej. "10d")
            return $"https://openweathermap.org/img/wn/{iconOrUrl}@2x.png";
        }

        private async Task CargarClimaAsync()
        {
            if (_isBusy) return;

            try
            {
                SetBusy(true);

                // Clima actual
                var climaActual = await OpenWeatherService.ObtenerClimaActualAsync();

                lblFechaActual.Text = climaActual.Fecha;
                lblTempActual.Text = $"{climaActual.Temperatura:0} °C"; // <-- sin decimales
                lblDescripcionActual.Text = climaActual.Descripcion;

                // Icono actual: forzar URL https y cache
                var iconUrl = ToIconUrl(climaActual.Icono);
                imgIconoActual.Source = new UriImageSource
                {
                    Uri = string.IsNullOrWhiteSpace(iconUrl) ? null : new Uri(iconUrl),
                    CachingEnabled = true,
                    CacheValidity = TimeSpan.FromHours(6)
                };

                // Pronóstico
                var pronostico = await OpenWeatherService.ObtenerPronosticoAsync();

                // Normalizar iconos del pronóstico (HTTPS / código -> URL)
                if (pronostico != null)
                {
                    foreach (var item in pronostico)
                    {
                        // Asumimos que el modelo tiene propiedad Icono settable
                        var tipo = item.GetType();
                        var prop = tipo.GetProperty("Icono");
                        if (prop != null && prop.CanWrite)
                        {
                            var val = prop.GetValue(item) as string;
                            prop.SetValue(item, ToIconUrl(val));
                        }
                    }
                }

                climaCollection.ItemsSource = pronostico;

                // Última actualización
                lblActualizado.Text = $"Actualizado: {DateTime.Now:dd/MM/yyyy HH:mm}";
            }
            catch (Exception ex)
            {
                await DisplayAlert("Clima", "No se pudo cargar el clima.\n" + ex.Message, "OK");
            }
            finally
            {
                SetBusy(false);
            }
        }

        private async void RefreshView_Refreshing(object sender, EventArgs e)
        {
            await CargarClimaAsync();
            if (sender is RefreshView rv) rv.IsRefreshing = false;
        }

        private async void Reintentar_Clicked(object sender, EventArgs e)
        {
            await CargarClimaAsync();
        }

        private async void Volver_Clicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//MainPage");
        }
    }
}
