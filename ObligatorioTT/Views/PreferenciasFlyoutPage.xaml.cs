using System;
using Microsoft.Maui.Controls;
using ObligatorioTT.Services;

namespace ObligatorioTT.Views
{
    public partial class PreferenciasFlyoutPage : ContentPage
    {
        private bool _isSaving;

        public PreferenciasFlyoutPage()
        {
            InitializeComponent();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            // Cargar estado actual de switches
            swClima.IsToggled = FlyoutPreferences.Get(FlyoutPreferences.ShowClima, true);
            swCotizaciones.IsToggled = FlyoutPreferences.Get(FlyoutPreferences.ShowCotizaciones, true);
            swNoticias.IsToggled = FlyoutPreferences.Get(FlyoutPreferences.ShowNoticias, true);
            swPeliculas.IsToggled = FlyoutPreferences.Get(FlyoutPreferences.ShowPeliculas, true);
            swPatrocinadores.IsToggled = FlyoutPreferences.Get(FlyoutPreferences.ShowPatrocinadores, true);
            swMapa.IsToggled = FlyoutPreferences.Get(FlyoutPreferences.ShowMapa, true);
        }

        private async void Guardar_Clicked(object sender, EventArgs e)
        {
            if (_isSaving) return; // evita doble click
            _isSaving = true;
            try
            {
                // Guardar valores
                FlyoutPreferences.Set(FlyoutPreferences.ShowClima, swClima.IsToggled);
                FlyoutPreferences.Set(FlyoutPreferences.ShowCotizaciones, swCotizaciones.IsToggled);
                FlyoutPreferences.Set(FlyoutPreferences.ShowNoticias, swNoticias.IsToggled);
                FlyoutPreferences.Set(FlyoutPreferences.ShowPeliculas, swPeliculas.IsToggled);
                FlyoutPreferences.Set(FlyoutPreferences.ShowPatrocinadores, swPatrocinadores.IsToggled);
                FlyoutPreferences.Set(FlyoutPreferences.ShowMapa, swMapa.IsToggled);

                // Notificar al AppShell para refrescar el Flyout
                FlyoutPreferences.RaiseChanged();

                // Mostrar confirmación ANTES de cerrar el modal
                await DisplayAlert("Preferencias", "Preferencias guardadas con éxito.", "OK");

                // Cerrar el modal (como la página se abre modal)
                if (Navigation.ModalStack.Contains(this))
                    await Navigation.PopModalAsync(true);
                else
                    await Shell.Current.GoToAsync("..", true);
            }
            finally
            {
                _isSaving = false;
            }
        }
    }
}
