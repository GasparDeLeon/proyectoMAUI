using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;          // Preferences
using ObligatorioTT.Data;              // DatabaseService
using ObligatorioTT.Models;            // Usuario
using ObligatorioTT.Helpers;           // ServiceHelper

namespace ObligatorioTT
{
    public partial class MainPage : ContentPage
    {
        private readonly DatabaseService _db;

        public MainPage()
        {
            InitializeComponent();
            _db = ServiceHelper.GetService<DatabaseService>();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await ActualizarBienvenidaAsync();
            _ = RunEntranceAnimationsAsync();
        }

        private async Task ActualizarBienvenidaAsync()
        {
            try
            {
                var userId = Preferences.Get("LoggedUserId", 0);
                Usuario? usuario = null;

                if (userId > 0)
                {
                    var lista = await _db.GetUsuariosAsync();
                    usuario = lista.FirstOrDefault(u => u.Id == userId);
                }

                if (usuario == null)
                {
                    var userName = Preferences.Get("LoggedUser", string.Empty);
                    if (!string.IsNullOrWhiteSpace(userName))
                        usuario = await _db.GetUsuarioByUserAsync(userName);
                }

                if (usuario != null)
                    lblBienvenida.Text = $"Bienvenido, {usuario.NombreCompleto}";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MainPage.ActualizarBienvenidaAsync] {ex}");
            }
        }

        // Click del botón (placeholder hasta conectar el stream)
        private async void OnPlayClicked(object sender, EventArgs e)
        {
            await DisplayAlert("Reproductor", "La señal en vivo estará disponible pronto.", "OK");
        }

        // Animaciones sutiles y “pulso” de la barra decorativa
        private async Task RunEntranceAnimationsAsync()
        {
            try
            {
                var hero = this.FindByName<Border>("heroBorder");
                var card = this.FindByName<Frame>("playerCard");
                var info = this.FindByName<Frame>("infoCard");
                var bar = this.FindByName<View>("progressPulse");

                if (hero != null) hero.Opacity = 0;
                if (card != null)
                {
                    card.Opacity = 0;
                    card.Scale = 0.98;
                }
                if (info != null) info.Opacity = 0;

                if (hero != null) await hero.FadeTo(1, 220, Easing.CubicOut);
                if (card != null)
                {
                    await Task.WhenAll(
                        card.FadeTo(1, 300, Easing.CubicOut),
                        card.ScaleTo(1, 300, Easing.CubicOut)
                    );
                }
                if (info != null) await info.FadeTo(1, 240, Easing.CubicOut);

                // Pulso decorativo
                if (bar != null)
                {
                    if (bar.Width <= 0) await Task.Delay(60);
                    while (IsVisible)
                    {
                        await bar.TranslateTo(90, 0, 900, Easing.CubicInOut);
                        await bar.TranslateTo(0, 0, 900, Easing.CubicInOut);
                    }
                }
            }
            catch
            {
                // Solo visual
            }
        }
    }
}
