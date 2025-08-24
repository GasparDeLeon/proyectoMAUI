using System.Globalization;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json.Linq;
using Microsoft.Maui.Storage;
using Microsoft.Maui.Controls;

namespace ObligatorioTT.Views;

public partial class CotizacionesPage : ContentPage
{
    private CancellationTokenSource? _cts;
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(15) };
    private readonly CultureInfo _uy = new("es-UY");

    private const string LAST_UPDATE_KEY = "Cotizaciones_LastUpdate"; // epoch seconds (UTC)
    private static readonly string CacheFilePath =
        Path.Combine(FileSystem.AppDataDirectory, "cotizaciones_cache.json");

    public CotizacionesPage()
    {
        InitializeComponent();
        // Carga inicial: nunca fuerza. Si ya se actualizó hoy, solo muestra caché.
        _ = CargarCotizacionesAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _cts?.Cancel();
    }

    private static bool DebeActualizar()
    {
        var lastEpoch = Preferences.Get(LAST_UPDATE_KEY, 0L);
        if (lastEpoch == 0) return true;

        var lastLocalDate = DateTimeOffset.FromUnixTimeSeconds(lastEpoch).ToLocalTime().Date;
        var hoyLocalDate = DateTimeOffset.Now.Date;
        return lastLocalDate != hoyLocalDate;
    }

    /// <summary>
    /// Muestra caché y SOLO llama a la API si corresponde por día.
    /// No hay manera de forzar desde UI.
    /// </summary>
    private async Task CargarCotizacionesAsync()
    {
        if (_cts != null) return;
        _cts = new CancellationTokenSource();

        try
        {
            lblEstado.Text = "Cargando…";
            spinner.IsVisible = spinner.IsRunning = true;

            // 1) Si hay caché, muéstrala de inmediato (mejor UX)
            if (File.Exists(CacheFilePath))
            {
                var jsonCache = await File.ReadAllTextAsync(CacheFilePath);
                AplicarDatosAUI(jsonCache);

                var lastEpoch = Preferences.Get(LAST_UPDATE_KEY, 0L);
                DateTime? lastLocal = lastEpoch > 0
                    ? DateTimeOffset.FromUnixTimeSeconds(lastEpoch).ToLocalTime().DateTime
                    : (DateTime?)null;
                ActualizarLabelsFecha(lastLocal);
            }

            // 2) ¿Toca actualizar hoy?
            if (!DebeActualizar())
            {
                lblEstado.Text = "";
                return; // ya actualizada hoy ? NO llamamos a la API
            }

            // 3) Llamada a la API (solo si tocaba)
            const string accessKey = "507ee1905a93063864b39e802e1dee7d";
            string url = $"https://api.currencylayer.com/live?access_key={accessKey}&currencies=UYU,EUR,BRL&format=1";

            using var resp = await _http.GetAsync(url, _cts.Token);
            resp.EnsureSuccessStatusCode();
            var response = await resp.Content.ReadAsStringAsync();

            // Cachear tal cual
            Directory.CreateDirectory(Path.GetDirectoryName(CacheFilePath)!);
            await File.WriteAllTextAsync(CacheFilePath, response, Encoding.UTF8);
            Preferences.Set(LAST_UPDATE_KEY, DateTimeOffset.UtcNow.ToUnixTimeSeconds());

            // Aplicar nuevos datos
            AplicarDatosAUI(response);
            ActualizarLabelsFecha(DateTimeOffset.UtcNow.ToLocalTime().DateTime);

            lblEstado.Text = "";
        }
        catch (TaskCanceledException)
        {
            lblEstado.Text = "La consulta fue cancelada.";
        }
        catch (HttpRequestException)
        {
            // Sin red: mostrar caché si existe (ya se mostró en el paso 1)
            lblEstado.Text = File.Exists(CacheFilePath)
                ? "Mostrando datos en caché."
                : "Error de red al cargar cotizaciones.";
        }
        catch
        {
            lblEstado.Text = File.Exists(CacheFilePath)
                ? "Mostrando datos en caché."
                : "Error al cargar cotizaciones.";
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
            throw new InvalidOperationException(data["error"]?["info"]?.ToString() ?? "Respuesta inválida.");

        var quotes = data["quotes"]!;
        decimal usd = quotes["USDUYU"]!.Value<decimal>();
        decimal eur = quotes["USDEUR"]!.Value<decimal>();
        decimal brl = quotes["USDBRL"]!.Value<decimal>();

        decimal eurToUyu = usd / eur;
        decimal brlToUyu = usd / brl;

        lblUSD.Text = $"{usd.ToString("N2", _uy)} UYU";
        lblEUR.Text = $"{eurToUyu.ToString("N2", _uy)} UYU";
        lblBRL.Text = $"{brlToUyu.ToString("N2", _uy)} UYU";

        // Guardar para el widget de Inicio + broadcast
        Preferences.Set("UltimoUSD", (double)usd);
        Preferences.Set("UltimoUSD_TS", DateTime.UtcNow.ToString("o"));
        MessagingCenter.Send<object, double>(this, "USD_UPDATED", (double)usd);
    }

    private void ActualizarLabelsFecha(DateTime? fechaLocal)
    {
        var texto = fechaLocal.HasValue
            ? fechaLocal.Value.ToString("dd/MM/yyyy HH:mm", _uy)
            : "-";
        lblUpdatedBottom.Text = $"Actualizado: {texto}";
    }

    // Si tenés un botón "Actualizar" apuntado a este handler,
    // ya NO fuerza: simplemente vuelve a llamar al flujo normal (respetando DebeActualizar).
    private async void OnActualizarClicked(object sender, EventArgs e)
    {
        await CargarCotizacionesAsync();
        // opcional: si NO corresponde actualizar hoy, podrías avisar:
        // if (!DebeActualizar()) await DisplayAlert("Cotizaciones", "Ya se actualizó hoy.", "OK");
    }
}
