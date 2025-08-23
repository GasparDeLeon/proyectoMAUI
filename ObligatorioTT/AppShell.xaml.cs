using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Maui.Controls;        
using Microsoft.Maui.Storage;       

using ObligatorioTT.Services;        
using ObligatorioTT.Views;           

namespace ObligatorioTT
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            Routing.RegisterRoute(nameof(SponsorFormPage), typeof(SponsorFormPage));
            Routing.RegisterRoute(nameof(PreferenciasFlyoutPage), typeof(PreferenciasFlyoutPage));
            Routing.RegisterRoute(nameof(SponsorsPage), typeof(SponsorsPage));
            Routing.RegisterRoute(nameof(TrailerPage), typeof(TrailerPage));

#if ANDROID
            // Ruta SOLO Android: selector de pin en mapa
            Routing.RegisterRoute("PinPickerPage", typeof(PinPickerPage));
#endif

            FlyoutPreferences.PreferencesChanged += () =>
            {
                if (Dispatcher != null)
                    Dispatcher.Dispatch(ApplyFlyoutVisibility);
                else
                    ApplyFlyoutVisibility();
            };

            ApplyFlyoutVisibility();
        }

        private void ApplyFlyoutVisibility()
        {
            if (RootItem == null) return;

            NormalizeFlyout();

            if (PerfilItem != null) PerfilItem.IsVisible = true;

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

            var desired = new List<ShellContent?>
            {
                InicioItem,
                PerfilItem,  
                ClimaItem, CotizacionesItem, NoticiasItem,
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

            void NormalizeFlyout()
            {
                var known = new HashSet<ShellContent?>(new[]
                {
                    PerfilItem,
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

        private async void OnPreferenciasClicked(object sender, EventArgs e)
        {
            if (Shell.Current?.Navigation?.ModalStack?.Count > 0)
                return;

            await Shell.Current.GoToAsync(nameof(PreferenciasFlyoutPage), true);
            Shell.Current.FlyoutIsPresented = false;
        }

        private void OnPreferenciasFooterTapped(object sender, TappedEventArgs e)
            => OnPreferenciasClicked(sender, EventArgs.Empty);

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
