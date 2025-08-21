using System.Globalization;
using System.Net.Http;
using Newtonsoft.Json.Linq;

namespace ObligatorioTT.Views;

public partial class CotizacionesPage : ContentPage
{
    // >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
    // Campos necesarios
    private CancellationTokenSource? _cts;
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(15) };
    private readonly CultureInfo _uy = new("es-UY");
    // <<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<

    public CotizacionesPage()
    {
        InitializeComponent();
        // Llamada inicial correcta (async)
        _ = ActualizarCotizacionesAsync();
    }

    // Evento de RefreshView (pull-to-refresh)
    private async void RefreshView_Refreshing(object? sender, EventArgs e)
    {
        await ActualizarCotizacionesAsync();
        if (sender is RefreshView rv) rv.IsRefreshing = false;
    }

    // Botón "Actualizar cotizaciones"
    private async void Actualizar_Clicked(object sender, EventArgs e)
    {
        await ActualizarCotizacionesAsync();
    }

    private async Task ActualizarCotizacionesAsync()
    {
        // Evitar múltiples llamadas simultáneas
        if (_cts != null) return;
        _cts = new CancellationTokenSource();

        try
        {
            lblEstado.Text = "Cargando…";
            spinner.IsVisible = spinner.IsRunning = true;

            // Plan free de CurrencyLayer => usar http
            const string accessKey = "56b50286bf0f1c812248d1adc9622d57";
            string url = $"http://api.currencylayer.com/live?access_key={accessKey}&currencies=UYU,EUR,BRL&format=1";

            var response = await _http.GetStringAsync(url, _cts.Token);
            var json = JObject.Parse(response);

            if (json["success"]?.Value<bool>() == true)
            {
                var quotes = json["quotes"]!;

                decimal usdToUyu = quotes["USDUYU"]!.Value<decimal>();
                decimal usdToEur = quotes["USDEUR"]!.Value<decimal>();
                decimal usdToBrl = quotes["USDBRL"]!.Value<decimal>();

                decimal eurToUyu = usdToUyu / usdToEur;
                decimal brlToUyu = usdToUyu / usdToBrl;

                
                lblUSD.Text = $"1 USD = {usdToUyu.ToString("N2", _uy)} UYU";
                lblEUR.Text = $"1 EUR = {eurToUyu.ToString("N2", _uy)} UYU";
                lblBRL.Text = $"1 BRL = {brlToUyu.ToString("N2", _uy)} UYU";


                lblFecha.Text = $"Última actualización: {DateTime.Now.ToString("dd/MM/yyyy HH:mm", _uy)}";
                lblEstado.Text = "";
            }
            else
            {
                string error = json["error"]?["info"]?.ToString() ?? "Error desconocido.";
                lblEstado.Text = "No se pudieron obtener las cotizaciones.";
                await DisplayAlert("Error", error, "OK");
            }
        }
        catch (TaskCanceledException)
        {
            lblEstado.Text = "La consulta fue cancelada.";
        }
        catch (HttpRequestException ex)
        {
            lblEstado.Text = "Error de red al cargar cotizaciones.";
            await DisplayAlert("Red", ex.Message, "OK");
        }
        catch (Exception ex)
        {
            lblEstado.Text = "Error al cargar cotizaciones.";
            await DisplayAlert("Excepción", ex.Message, "OK");
        }
        finally
        {
            spinner.IsVisible = spinner.IsRunning = false;
            _cts?.Dispose();
            _cts = null;
        }
    }
}
