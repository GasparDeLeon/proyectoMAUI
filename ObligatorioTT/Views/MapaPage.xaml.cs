#if ANDROID || IOS
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using Microsoft.Maui.Devices.Sensors; // Location
#endif
using Microsoft.Maui.Controls;

namespace ObligatorioTT.Views
{
    public partial class MapaPage : ContentPage
    {
        public MapaPage()
        {
#if WINDOWS
  
            Content = new Grid
            {
                Padding = 20,
                Children =
                {
                    new Label
                    {
                        Text = "El mapa está disponible en Android. Soporte para Windows (Azure Maps) próximamente.",
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

#if ANDROID || IOS
           
         
            var pos = new Location(-34.9011, -56.1645);
            map.MoveToRegion(MapSpan.FromCenterAndRadius(pos, Distance.FromKilometers(3)));
            map.Pins.Add(new Pin { Label = "Prueba", Address = "Montevideo", Location = pos });
#endif
        }
    }
}
