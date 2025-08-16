using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;

#if ANDROID || IOS
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using Microsoft.Maui.Devices.Sensors;
#endif

namespace ObligatorioTT.Views
{
    public partial class MapaPage : ContentPage
    {
        public MapaPage()
        {
#if WINDOWS
           
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
            return;
#else
            InitializeComponent();
#endif

#if ANDROID || IOS
            // En Android/iOS seguimos usando el control de mapas nativo de MAUI
            var pos = new Location(-34.9011, -56.1645); // Montevideo
            map.MoveToRegion(MapSpan.FromCenterAndRadius(pos, Distance.FromKilometers(3)));
            map.Pins.Add(new Pin { Label = "Prueba", Address = "Montevideo", Location = pos });
#endif
        }

#if WINDOWS
        private async Task<View> CrearWebViewAzureMapsAsync()
        {
            using var stream = await FileSystem.OpenAppPackageFileAsync("azuremap.html");
            using var reader = new StreamReader(stream);
            var html = await reader.ReadToEndAsync();

        
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

