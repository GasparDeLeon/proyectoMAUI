using Microsoft.Maui.Controls;
using ObligatorioTT.Services;

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
                imgIconoActual.Source = climaActual.Icono;

                // Pronóstico
                var pronostico = await OpenWeatherService.ObtenerPronosticoAsync();
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
