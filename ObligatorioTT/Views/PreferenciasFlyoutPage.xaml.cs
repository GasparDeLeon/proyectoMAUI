using ObligatorioTT.Services;

namespace ObligatorioTT.Views
{
    public partial class PreferenciasFlyoutPage : ContentPage
    {
        public PreferenciasFlyoutPage()
        {
            InitializeComponent();
            Cargar();
        }

        private void Cargar()
        {
            // Inicio ya no se toca: siempre visible en AppShell
            swClima.IsToggled = FlyoutPreferences.Get(FlyoutPreferences.ShowClima, true);
            swCotizaciones.IsToggled = FlyoutPreferences.Get(FlyoutPreferences.ShowCotizaciones, true);
            swNoticias.IsToggled = FlyoutPreferences.Get(FlyoutPreferences.ShowNoticias, true);
            swPeliculas.IsToggled = FlyoutPreferences.Get(FlyoutPreferences.ShowPeliculas, true);
            swPatrocinadores.IsToggled = FlyoutPreferences.Get(FlyoutPreferences.ShowPatrocinadores, true);
            swMapa.IsToggled = FlyoutPreferences.Get(FlyoutPreferences.ShowMapa, true);
        }

        private async void Guardar_Clicked(object sender, EventArgs e)
        {
            // Guardar estado de cada switch
            FlyoutPreferences.Set(FlyoutPreferences.ShowClima, swClima.IsToggled);
            FlyoutPreferences.Set(FlyoutPreferences.ShowCotizaciones, swCotizaciones.IsToggled);
            FlyoutPreferences.Set(FlyoutPreferences.ShowNoticias, swNoticias.IsToggled);
            FlyoutPreferences.Set(FlyoutPreferences.ShowPeliculas, swPeliculas.IsToggled);
            FlyoutPreferences.Set(FlyoutPreferences.ShowPatrocinadores, swPatrocinadores.IsToggled);
            FlyoutPreferences.Set(FlyoutPreferences.ShowMapa, swMapa.IsToggled);

            // Avisamos a AppShell que se refresque (manteniendo el orden original del XAML)
            FlyoutPreferences.RaiseChanged();

            // Pop-up en vez de label
            await DisplayAlert("Preferencias", "Cambios guardados correctamente.", "OK");
        }
    }
}
