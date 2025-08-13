using Microsoft.Maui.Controls;
using ObligatorioTT.Models;
using ObligatorioTT.Services;

namespace ObligatorioTT.Views
{
    public partial class ClimaPage : ContentPage
    {
        public ClimaPage()
        {
            InitializeComponent();
            CargarClima();
        }

        private async void CargarClima()
        {
            try
            {
                
                var climaActual = await OpenWeatherService.ObtenerClimaActualAsync();
                lblFechaActual.Text = climaActual.Fecha;
                lblTempActual.Text = $"{climaActual.Temperatura} °C";
                lblDescripcionActual.Text = climaActual.Descripcion;
                imgIconoActual.Source = climaActual.Icono;



                var pronostico = await OpenWeatherService.ObtenerPronosticoAsync();
                climaCollection.ItemsSource = pronostico;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "No se pudo cargar el clima.\n" + ex.Message, "OK");
            }
        }
        private async void Volver_Clicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//MainPage");
        }

    }
}
