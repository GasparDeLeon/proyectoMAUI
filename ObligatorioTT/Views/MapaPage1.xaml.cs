using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;

#if ANDROID || IOS
using Microsoft.Maui.Devices.Sensors;     // Location
using Microsoft.Maui.Maps;                 // MapSpan, Distance
using ControlsMaps = Microsoft.Maui.Controls.Maps; // Map, Pin (alias del namespace)
#endif

namespace ObligatorioTT.Views
{
    public partial class MapaPage1 : ContentPage
    {
        public MapaPage1()
        {
#if WINDOWS
            // En Windows usamos Azure Maps con un WebView (archivo html local)
            Content = new Label
            {
                Text = "Cargando mapa...",
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            };

            Dispatcher.Dispatch(async () =>
            {
                try
                {
                    Content = await CrearWebViewAzureMapsAsync();
                }
                catch (Exception ex)
                {
                    Content = new ScrollView
                    {
                        Content = new Label
                        {
                            Text = $"No se pudo cargar Azure Maps.\n\n{ex.Message}",
                            Margin = 20
                        }
                    };
                }
            });

            return; // Evita ejecutar el código de otras plataformas
#else
            InitializeComponent();
#endif

#if ANDROID || IOS
            // En Android/iOS usamos el control de mapas nativo de MAUI (declarado en el XAML con x:Name="map")
            if (this.FindByName<ControlsMaps.Map>("map") is ControlsMaps.Map map)
            {
                var pos = new Location(-34.9011, -56.1645); // Montevideo
                map.MoveToRegion(MapSpan.FromCenterAndRadius(pos, Distance.FromKilometers(3)));

                map.Pins.Add(new ControlsMaps.Pin
                {
                    Label = "Prueba",
                    Address = "Montevideo",
                    Location = pos
                });
            }
            else
            {
                Content = new Label
                {
                    Text = "No se encontró el control de mapa (x:Name=\"map\").",
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center
                };
            }
#endif
        }

#if WINDOWS
        private async Task<View> CrearWebViewAzureMapsAsync()
        {
            using var stream = await FileSystem.OpenAppPackageFileAsync("azuremap.html");
            using var reader = new StreamReader(stream);
            var html = await reader.ReadToEndAsync();

            // Reemplaza el placeholder por tu clave real (debes tener Secrets.AzureMapsKey definido)
            html = html.Replace("__AZURE_MAPS_KEY__", Secrets.AzureMapsKey);

            return new WebView
            {
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.Fill,
                Source = new HtmlWebViewSource { Html = html }
            };
        }
#endif
    }
}
