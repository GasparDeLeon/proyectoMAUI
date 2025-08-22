using System;
using System.Linq;
using System.Diagnostics;
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
            try
            {
                InitializeComponent();
                _db = ServiceHelper.GetService<DatabaseService>();
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR en MainPage: " + ex.Message);
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await ActualizarBienvenidaAsync();
        }

        private async Task ActualizarBienvenidaAsync()
        {
            try
            {
                var userId = Preferences.Get("LoggedUserId", 0);
                Usuario? usuario = null;

                if (userId > 0)
                {
                    // Usamos el método que ya tenés: GetUsuariosAsync + FirstOrDefault
                    var lista = await _db.GetUsuariosAsync();
                    usuario = lista.FirstOrDefault(u => u.Id == userId);
                }

                if (usuario == null)
                {
                    var userName = Preferences.Get("LoggedUser", string.Empty);
                    if (!string.IsNullOrWhiteSpace(userName))
                    {
                        // Fallback por username si hiciera falta
                        usuario = await _db.GetUsuarioByUserAsync(userName);
                    }
                }

                if (usuario != null && this.FindByName<Label>("lblBienvenida") is Label lbl)
                {
                    lbl.Text = $"Bienvenido, {usuario.NombreCompleto}";
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MainPage.ActualizarBienvenidaAsync] {ex}");
                // Silencioso para no molestar al usuario en la Home
            }
        }
    }
}
