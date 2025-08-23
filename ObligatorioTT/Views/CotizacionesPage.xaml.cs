using System.Globalization;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json.Linq;
using Microsoft.Maui.Storage;

namespace ObligatorioTT.Views;

public partial class CotizacionesPage : ContentPage
{
    private CancellationTokenSource? _cts;
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(15) };
    private readonly CultureInfo _uy = new("es-UY");

    // Cache + sello de tiempo
    private const string LAST_UPDATE_KEY = "Cotizaciones_LastUpdate"; // guarda epoch seconds (UTC)
    private static readonly string CacheFilePath = Path.Combine(FileSystem.AppDataDirectory, "cotizaciones_cache.json");

    public CotizacionesPage()
    {
        InitializeComponent();
        // Carga inicial
        _ = CargarCotizacionesAsync();
    }

    private static bool DebeActualizar()
    {
        var lastEpoch = Preferences.Get(LAST_UPDATE_KEY, 0L);
        if (lastEpoch == 0) return true;

        var lastLocalDate = DateTimeOffset.FromUnixTimeSeconds(lastEpoch).ToLocalTime().Date;
        var hoyLocalDate = DateTimeOffset.Now.Date;
        return lastLocalDate != hoyLocalDate;
    }

    private async Task CargarCotizacionesAsync(bool force = false)
    {
        if (_cts != null) return;
        _cts = new CancellationTokenSource();

        try
        {
            lblEstado.Text = "Cargando…";
            spinner.IsVisible = spinner.IsRunning = true;

            // ¿Podemos usar cache?
            if (!force && !DebeActualizar() && File.Exists(CacheFilePath))
            {
                var jsonCache = await File.ReadAllTextAsync(CacheFilePath);
                AplicarDatosAUI(jsonCache);

                var lastEpoch = Preferences.Get(LAST_UPDATE_KEY, 0L);
                if (lastEpoch > 0)
                {
                    var lastLocal = DateTimeOffset.FromUnixTimeSeconds(lastEpoch).ToLocalTime().DateTime;
                    ActualizarLabelsFecha(lastLocal);
                }
                else
                {
                    ActualizarLabelsFecha(null);
                }

                lblEstado.Text = "";
                return;
            }

            // Llamada a CurrencyLayer (ajustá tu key/endpoint si corresponde)
            const string accessKey = "507ee1905a93063864b39e802e1dee7d";
            string url = $"https://api.currencylayer.com/live?access_key={accessKey}&currencies=UYU,EUR,BRL&format=1";

            var response = await _http.GetStringAsync(url, _cts.Token);

            // Guardar cache "tal cual"
            Directory.CreateDirectory(Path.GetDirectoryName(CacheFilePath)!);
            await File.WriteAllTextAsync(CacheFilePath, response, Encoding.UTF8);

            // Guardar sello de tiempo (UTC epoch seconds)
            var nowUtc = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            Preferences.Set(LAST_UPDATE_KEY, nowUtc);

            // Aplicar a UI
            AplicarDatosAUI(response);
            ActualizarLabelsFecha(DateTimeOffset.UtcNow.ToLocalTime().DateTime);

            lblEstado.Text = "";
        }
        catch (TaskCanceledException)
        {
            lblEstado.Text = "La consulta fue cancelada.";
        }
        catch (HttpRequestException ex)
        {
            // Fallback a cache si existe
            if (File.Exists(CacheFilePath))
            {
                var jsonCache = await File.ReadAllTextAsync(CacheFilePath);
                AplicarDatosAUI(jsonCache);

                var lastEpoch = Preferences.Get(LAST_UPDATE_KEY, 0L);
                var lastLocal = lastEpoch > 0
                    ? DateTimeOffset.FromUnixTimeSeconds(lastEpoch).ToLocalTime().DateTime
                    : (DateTime?)null;
                ActualizarLabelsFecha(lastLocal);

                lblEstado.Text = "Mostrando datos en caché (sin conexión).";
            }
            else
            {
                lblEstado.Text = "Error de red al cargar cotizaciones.";
                await DisplayAlert("Red", ex.Message, "OK");
            }
        }
        catch (Exception ex)
        {
            // Fallback a cache si existe
            if (File.Exists(CacheFilePath))
            {
                var jsonCache = await File.ReadAllTextAsync(CacheFilePath);
                AplicarDatosAUI(jsonCache);

                var lastEpoch = Preferences.Get(LAST_UPDATE_KEY, 0L);
                var lastLocal = lastEpoch > 0
                    ? DateTimeOffset.FromUnixTimeSeconds(lastEpoch).ToLocalTime().DateTime
                    : (DateTime?)null;
                ActualizarLabelsFecha(lastLocal);

                lblEstado.Text = "Mostrando datos en caché (error de servicio).";
            }
            else
            {
                lblEstado.Text = "Error al cargar cotizaciones.";
                await DisplayAlert("Excepción", ex.Message, "OK");
            }
        }
        finally
        {
            spinner.IsVisible = spinner.IsRunning = false;
            _cts?.Dispose();
            _cts = null;
        }
    }

    private void AplicarDatosAUI(string json)
    {
        var data = JObject.Parse(json);

        if (data["success"]?.Value<bool>() != true)
        {
            var error = data["error"]?["info"]?.ToString() ?? "Respuesta inválida.";
            throw new InvalidOperationException(error);
        }

        var quotes = data["quotes"]!;
        decimal usdToUyu = quotes["USDUYU"]!.Value<decimal>();
        decimal usdToEur = quotes["USDEUR"]!.Value<decimal>();
        decimal usdToBrl = quotes["USDBRL"]!.Value<decimal>();

        // Convertimos EUR/BRL a UYU a partir de USD:
        decimal eurToUyu = usdToUyu / usdToEur;
        decimal brlToUyu = usdToUyu / usdToBrl;

        lblUSD.Text = $"{usdToUyu.ToString("N2", _uy)} UYU";
        lblEUR.Text = $"{eurToUyu.ToString("N2", _uy)} UYU";
        lblBRL.Text = $"{brlToUyu.ToString("N2", _uy)} UYU";
    }

    private void ActualizarLabelsFecha(DateTime? fechaLocal)
    {
        var texto = fechaLocal.HasValue
            ? fechaLocal.Value.ToString("dd/MM/yyyy HH:mm", _uy)
            : "-";

        if (lblUpdatedBottom != null)
            lblUpdatedBottom.Text = $"Actualizado: {texto}";
    }
}
