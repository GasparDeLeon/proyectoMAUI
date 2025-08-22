#if ANDROID || IOS
using Microsoft.Maui.Controls.Maps;    // Map
using Microsoft.Maui.Maps;             // MapSpan, Distance
using Microsoft.Maui.Devices.Sensors;  // Location, Geolocation
using Microsoft.Maui.ApplicationModel; // Permissions
#endif
using Microsoft.Maui.Controls;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ObligatorioTT.Views
{
    public partial class MapaPage : ContentPage
    {
        public MapaPage()
        {
#if WINDOWS
            // Vista de cortesía en Windows (sin mapas reales)
            Content = new Grid
            {
                Padding = 20,
                Children =
                {
                    new Label
                    {
                        Text = "El mapa está disponible en Android. Soporte para Windows (Azure/WinUI Maps) próximamente.",
                        HorizontalOptions = LayoutOptions.Center,
                        VerticalOptions = LayoutOptions.Center,
                        HorizontalTextAlignment = TextAlignment.Center
                    }
                }
            };
            return;
#else
            InitializeComponent();
#endif
        }

        // Handler accesible en todas las plataformas (para que compile el XAML en Windows)
        private async void Recenter_Clicked(object? sender, EventArgs e)
        {
#if ANDROID || IOS
            await TryCenterOnUserAsync(force: true);
#else
            // Windows: sin acción
            // await DisplayAlert("Mapa", "Recentrar no está disponible en Windows.", "OK");
#endif
        }

#if ANDROID || IOS
        protected override async void OnAppearing()
        {
            base.OnAppearing();

#if ANDROID
            // 1) Mostrar sponsors y encuadrar desde el primer frame (evita "salto" a otro país)
            await MapaPageAndroidHelpers.LoadSponsorPinsAsync(map, adjustCamera: true);
#endif

            // 2) Luego, centrar en el usuario en segundo plano (suaviza transición)
            _ = TryCenterOnUserAsync(); // fire-and-forget
        }

        private async Task TryCenterOnUserAsync(bool force = false)
        {
            try
            {
                // 1) Permiso de ubicación
                var granted = await EnsureLocationPermissionAsync();
                if (!granted)
                {
                    await DisplayAlert("Ubicación", "No se otorgó permiso de ubicación. No puedo centrar el mapa.", "OK");
                    return;
                }

                // 2) Ubicación actual con estrategia LastKnown->Fresh
                var location = await GetCurrentLocationAsync();
                if (location == null)
                {
                    await DisplayAlert("Ubicación", "No se pudo obtener la ubicación actual.", "OK");
                    return;
                }

                // 3) Centrar mapa en el usuario (solo punto azul; sin pin adicional)
                await CenterMapOnAsync(location, radiusKm: 1.0);

                // 4) Pines de sponsors (ya cargados; no movemos cámara)
#if ANDROID
                await MapaPageAndroidHelpers.LoadSponsorPinsAsync(map, adjustCamera: false);
#endif
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Mapa] Error al centrar en usuario: {ex}");
                if (force)
                    await DisplayAlert("Mapa", "Ocurrió un error al recentrar el mapa.", "OK");
            }
        }

        private async Task<bool> EnsureLocationPermissionAsync()
        {
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (status == PermissionStatus.Granted)
                return true;

            if (status == PermissionStatus.Denied && Permissions.ShouldShowRationale<Permissions.LocationWhenInUse>())
            {
                await DisplayAlert("Permiso requerido",
                    "Necesito acceder a tu ubicación para centrar el mapa en tu posición actual.",
                    "OK");
            }

            status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            return status == PermissionStatus.Granted;
        }

        private async Task<Location?> GetCurrentLocationAsync()
        {
            try
            {
                // 1) Entregar rápido si hay última conocida (evita salto)
                var last = await Geolocation.Default.GetLastKnownLocationAsync();
                if (last != null)
                {
                    // Disparar mejora en background (fresh)
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            var ctsBg = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                            var reqBg = new GeolocationRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(10));
                            var fresh = await Geolocation.Default.GetLocationAsync(reqBg, ctsBg.Token);
                            if (fresh != null)
                            {
                                // Reciente refinamiento suave de la región
                                await CenterMapOnAsync(fresh, radiusKm: 1.0);
                            }
                        }
                        catch { /* ignoramos errores de la actualización en bg */ }
                    });

                    return last;
                }

                // 2) Si no hay last-known, pedir una fresh con timeout
                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                var request = new GeolocationRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(10));
                return await Geolocation.Default.GetLocationAsync(request, cts.Token);
            }
            catch (FeatureNotEnabledException)
            {
                // GPS desactivado: intentar última conocida
                return await Geolocation.Default.GetLastKnownLocationAsync();
            }
            catch
            {
                return null;
            }
        }

        private async Task CenterMapOnAsync(Location location, double radiusKm)
        {
            if (map == null) return;

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                var center = new MapSpan(
                    new Location(location.Latitude, location.Longitude),
                    radiusKm / 111.0,   // aprox grados por km
                    radiusKm / 111.0);

                map.MoveToRegion(center);
            });
        }
#endif
    }
}
