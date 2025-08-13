using Newtonsoft.Json.Linq;
using System.Net.Http;

namespace ObligatorioTT.Views;

public partial class CotizacionesPage : ContentPage
{
    public CotizacionesPage()
    {
        InitializeComponent();
        ActualizarCotizaciones(); 
    }

    private async void ActualizarCotizaciones()
    {
        try
        {
            string accessKey = "507ee1905a93063864b39e802e1dee7d";
            string url = $"https://api.currencylayer.com/live?access_key={accessKey}&currencies=UYU,EUR,BRL&format=1";

            using HttpClient client = new();
            var response = await client.GetStringAsync(url);


            var json = JObject.Parse(response);

            if (json["success"]?.Value<bool>() == true)
            {
                var quotes = json["quotes"];

                
                decimal usdToUyu = quotes["USDUYU"].Value<decimal>();
                decimal usdToEur = quotes["USDEUR"].Value<decimal>();
                decimal usdToBrl = quotes["USDBRL"].Value<decimal>();

              
                decimal eurToUyu = usdToUyu / usdToEur;
                decimal brlToUyu = usdToUyu / usdToBrl;

                
                lblUSD.Text = $"1 USD = {usdToUyu:0.00} UYU";
                lblEUR.Text = $"1 EUR = {eurToUyu:0.00} UYU";
                lblBRL.Text = $"1 BRL = {brlToUyu:0.00} UYU";

                lblFecha.Text = $"Última actualización: {DateTime.Now:dd/MM/yyyy HH:mm}";
            }
            else
            {
                string error = json["error"]?["info"]?.ToString() ?? "Error desconocido.";
                await DisplayAlert("Error", error, "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Excepción", ex.Message, "OK");
        }
    }

    
    private void Actualizar_Clicked(object sender, EventArgs e)
    {
        ActualizarCotizaciones();
    }
}
