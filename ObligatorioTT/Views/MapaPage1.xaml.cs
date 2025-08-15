using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;

namespace ObligatorioTT.Views
{
    public partial class MapaPage : ContentPage
    {
        public MapaPage()
        {
            InitializeComponent();

            // Centro de prueba (Montevideo) + un pin
            var pos = new Location(-34.9011, -56.1645);
            map.MoveToRegion(MapSpan.FromCenterAndRadius(pos, Distance.FromKilometers(3)));
            map.Pins.Add(new Pin { Label = "Prueba", Address = "Montevideo", Location = pos });
        }
    }
}
