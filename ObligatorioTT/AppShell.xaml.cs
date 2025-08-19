using System.Collections.Generic;
using System.Linq;
using Microsoft.Maui.Storage;         // Preferences
using ObligatorioTT.Services;         // FlyoutPreferences
using ObligatorioTT.Views;

namespace ObligatorioTT
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            // Rutas existentes
           
            Routing.RegisterRoute(nameof(SponsorFormPage), typeof(SponsorFormPage));

            // Ruta para la pantalla de preferencias (página normal)
            Routing.RegisterRoute(nameof(PreferenciasFlyoutPage), typeof(PreferenciasFlyoutPage));

            // Aplico visibilidad en base a preferencias
            ApplyFlyoutVisibility();

            // Si cambian las preferencias, refrescamos el flyout (UI thread)
            FlyoutPreferences.PreferencesChanged += () =>
            {
                Dispatcher.Dispatch(ApplyFlyoutVisibility);
            };
        }

        private void ApplyFlyoutVisibility()
        {
            if (RootItem == null) return;

            // Normaliza por si algo quedó fuera del RootItem
            NormalizeFlyout();

            // Inicio siempre visible
            if (InicioItem != null) InicioItem.IsVisible = true;

            if (ClimaItem != null)
                ClimaItem.IsVisible = FlyoutPreferences.Get(FlyoutPreferences.ShowClima, true);
            if (CotizacionesItem != null)
                CotizacionesItem.IsVisible = FlyoutPreferences.Get(FlyoutPreferences.ShowCotizaciones, true);
            if (NoticiasItem != null)
                NoticiasItem.IsVisible = FlyoutPreferences.Get(FlyoutPreferences.ShowNoticias, true);
            if (PeliculasItem != null)
                PeliculasItem.IsVisible = FlyoutPreferences.Get(FlyoutPreferences.ShowPeliculas, true);
            if (PatrocinadoresItem != null)
                PatrocinadoresItem.IsVisible = FlyoutPreferences.Get(FlyoutPreferences.ShowPatrocinadores, true);
            if (MapaItem != null)
                MapaItem.IsVisible = FlyoutPreferences.Get(FlyoutPreferences.ShowMapa, true);

            // Reordenar EN SITIO dentro de RootItem (sin Clear)
            var desired = new List<ShellContent?>
            {
                InicioItem, ClimaItem, CotizacionesItem, NoticiasItem,
                PeliculasItem, PatrocinadoresItem, MapaItem
            };

            int target = 0;
            foreach (var item in desired)
            {
                if (item == null) continue;

                int current = RootItem.Items.IndexOf(item);
                if (current == -1)
                {
                    if (target >= RootItem.Items.Count) RootItem.Items.Add(item);
                    else RootItem.Items.Insert(target, item);
                }
                else if (current != target)
                {
                    RootItem.Items.RemoveAt(current);
                    if (target >= RootItem.Items.Count) RootItem.Items.Add(item);
                    else RootItem.Items.Insert(target, item);
                }
                target++;
            }

            // ---- función local: trae contenidos conocidos al RootItem, sin borrar contenedores ----
            void NormalizeFlyout()
            {
                var known = new HashSet<ShellContent?>(new[]
                {
                    InicioItem, ClimaItem, CotizacionesItem, NoticiasItem,
                    PeliculasItem, PatrocinadoresItem, MapaItem
                });

                foreach (var shellItem in this.Items.ToList())
                {
                    if (shellItem == RootItem) continue;

                    foreach (var section in shellItem.Items.ToList())
                    {
                        foreach (var content in section.Items.ToList())
                        {
                            if (known.Contains(content))
                            {
                                section.Items.Remove(content);
                                if (!RootItem.Items.Contains(content))
                                    RootItem.Items.Add(content);
                            }
                        }
                    }
                }
            }
        }

        // ---------- Preferencias (navegación clásica) ----------
        private async void OnPreferenciasClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync(nameof(PreferenciasFlyoutPage));
            Shell.Current.FlyoutIsPresented = false; // cerrar flyout
        }

        private void OnPreferenciasFooterTapped(object sender, TappedEventArgs e)
            => OnPreferenciasClicked(sender, EventArgs.Empty);

        // ---------- Cerrar sesión ----------
        private async void OnLogoutClicked(object sender, EventArgs e)
        {
            var confirm = await DisplayAlert("Cerrar sesión", "¿Seguro que querés salir?", "Sí", "No");
            if (!confirm) return;

            Preferences.Remove("LoggedUser");
            Preferences.Remove("LoggedUserId");

            Application.Current.MainPage = new NavigationPage(new LoginPage());
        }

        private void OnCerrarSesionFooterTapped(object sender, TappedEventArgs e)
            => OnLogoutClicked(sender, EventArgs.Empty);
    }
}
