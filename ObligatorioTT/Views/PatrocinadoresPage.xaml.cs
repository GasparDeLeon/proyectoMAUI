using System;
using System.Linq;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.ApplicationModel;   // Launcher
using ObligatorioTT.Models;
using ObligatorioTT.Services;

namespace ObligatorioTT.Views
{
    public partial class PatrocinadoresPage : ContentPage
    {
        public PatrocinadoresPage()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await CargarSponsorsAsync();
        }

        private async Task CargarSponsorsAsync()
        {
            await SponsorRepository.Inst.InitAsync();

            // Si tu repo usa otro método (p. ej. ListAsync), cambialo aquí:
            var sponsors = await SponsorRepository.Inst.GetAllAsync();

            // Orden alfabético
            cvSponsors.ItemsSource = sponsors
                ?.OrderBy(s => s.Nombre)
                .ToList();
        }

        private async void OnAbrirMapaClicked(object sender, EventArgs e)
        {
#if ANDROID
    if (sender is not Button btn || btn.BindingContext is not Sponsor s)
        return;

    double? lat = s.Latitud, lng = s.Longitud;

    // Si no hay coordenadas pero hay dirección, intentar geocodificar
    if ((!lat.HasValue || !lng.HasValue) && !string.IsNullOrWhiteSpace(s.Direccion))
    {
        try
        {
            var results = await Microsoft.Maui.Devices.Sensors.Geocoding.GetLocationsAsync(s.Direccion);
            var loc = results?.FirstOrDefault();
            if (loc != null)
            {
                lat = loc.Latitude;
                lng = loc.Longitude;
            }
        }
        catch { /* ignoramos; manejamos abajo si sigue sin coords */ }
    }

    if (!lat.HasValue || !lng.HasValue)
    {
        await DisplayAlert("Mapa", "No hay ubicación válida para este patrocinador.", "OK");
        return;
    }

    var name = Uri.EscapeDataString(s.Nombre ?? "Patrocinador");
    var latStr = lat.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
    var lngStr = lng.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);

    // Navegación a tu página interna de mapa
    await Shell.Current.GoToAsync($"//MapaPage?lat={latStr}&lng={lngStr}&name={name}");

#else
            await DisplayAlert("Mapa", "Disponible solo en Android.", "OK");
#endif
        }

        private async void OnGestionarClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync(nameof(SponsorsPage));
        }

    }
}
