using Microsoft.Maui.Controls;

namespace ObligatorioTT.Views
{
    public partial class TrailerPage : ContentPage
    {
        public TrailerPage(string urlTrailer)
        {
            InitializeComponent();
            webTrailer.Source = urlTrailer; // usamos el link que ya traés de TMDb
        }
    }
}
