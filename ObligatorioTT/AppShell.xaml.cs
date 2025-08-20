using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Maui.Controls;        // Shell, ShellContent, DisplayAlert, TappedEventArgs, NavigationPage
using Microsoft.Maui.Storage;         // Preferences

using ObligatorioTT.Services;         // FlyoutPreferences
using ObligatorioTT.Views;            // LoginPage, PreferenciasFlyoutPage, etc.

namespace ObligatorioTT
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            // Rutas SOLO para páginas que NO están en el Flyout
            Routing.RegisterRoute(nameof(SponsorFormPage), typeof(SponsorFormPage));
            Routing.RegisterRoute(nameof(PreferenciasFlyoutPage), typeof(PreferenciasFlyoutPage));

#if ANDROID
            // Ruta SOLO Android: selector de pin en mapa
            Routing.RegisterRoute("PinPickerPage", typeof(PinPickerPage));
#endif

            // Suscribimos primero y luego aplicamos (si ya hay preferencias guardadas)
            FlyoutPreferences.PreferencesChanged += () =>
            {
                Dispatcher.Dispatch(ApplyFlyoutVisibility);
            };

            // Aplico visibilidad en base a preferencias
            ApplyFlyoutVisibility();
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

        // ---------- Preferencias (modal, desde el footer) ----------
        private async void OnPreferenciasClicked(object sender, EventArgs e)
        {
            // evita abrir otra preferencia encima si ya hay un modal
            if (Shell.Current?.Navigation?.ModalStack?.Count > 0)
                return;

            await Shell.Current.GoToAsync(nameof(PreferenciasFlyoutPage), true); // Modal (la página define PresentationMode)
            Shell.Current.FlyoutIsPresented = false; // cierra el flyout
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
