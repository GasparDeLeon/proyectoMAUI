using System.Collections.ObjectModel;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.IO;
using System.Linq;                       // <-- LINQ
using System.Threading.Tasks;            // <-- Task
using System.Windows.Input;              // <-- ICommand
using Microsoft.Maui.ApplicationModel;   // <-- MainThread, Launcher
using Microsoft.Maui.Controls;           // <-- ContentPage, ProgressBar, Label, SearchBar
using Microsoft.Maui.Storage;
using Newtonsoft.Json.Linq;

namespace ObligatorioTT.Views;

public partial class NoticiasPage : ContentPage
{
    private const string API_KEY = "pub_4b4e4d9e60da4fcb9c8073837a300312";
    private const string LAST_UPDATE_KEY = "UltimaActualizacionNoticias";
    private static readonly string CacheFilePath = Path.Combine(FileSystem.AppDataDirectory, "noticias_cache.json");

    private static readonly HttpClient http = new() { Timeout = TimeSpan.FromSeconds(10) };
    private readonly CultureInfo uy = new("es-UY");

    private bool _isLoading = false;
    private CancellationTokenSource? _cts;
    private CancellationTokenSource? _progressCts;
    private string? _nextPageToken = null;
    private string _queryActual = "uruguay";

    // Bindings
    public ObservableCollection<NoticiaItem> Items { get; } = new();

    private string _emptyMessage = "No hay noticias para mostrar.";
    public string EmptyMessage
    {
        get => _emptyMessage;
        set { if (_emptyMessage != value) { _emptyMessage = value; OnPropertyChanged(); } }
    }

    public class NoticiaItem
    {
        public string Titulo { get; set; } = "Sin título";
        public string Descripcion { get; set; } = "Sin descripción";
        public string Enlace { get; set; } = "";
        public string ImagenUrl { get; set; } = "";
        public string Fuente { get; set; } = "";
        public DateTime? FechaPub { get; set; }
    }

    public NoticiasPage()
    {
        InitializeComponent();
        BindingContext = this;

        CargarDesdeCacheSiHay();
        _ = LoadAsync(refresh: true);
    }

    // ---- Command para tap en cada card (usado por el XAML) ----
    public ICommand ItemTappedCommand => new Command<NoticiaItem>(async (item) =>
    {
        try
        {
            if (item == null) return;
            if (!TryNormalizeUrl(item.Enlace, out var uri) || uri == null) return;

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                if (await Launcher.Default.CanOpenAsync(uri))
                    await Launcher.Default.OpenAsync(uri);
                else
                    await Launcher.Default.OpenAsync(uri.ToString());
            });
        }
        catch (Exception ex)
        {
            await DisplayAlert("No se pudo abrir el enlace", ex.Message, "OK");
        }
    });

    // Helper: buscar control por nombre y castear seguro
    private T? Q<T>(string name) where T : class => this.FindByName(name) as T;

    // ---------------- Indicador (barra de progreso) ----------------
    private async Task SetLoading(bool on)
    {
        var bar = Q<ProgressBar>("busyBar");
        if (bar == null) return;

        if (on)
        {
            bar.IsVisible = true;
            bar.Progress = 0;
            _progressCts?.Cancel();
            _progressCts = new CancellationTokenSource();

            _ = Task.Run(async () =>
            {
                try
                {
                    while (!_progressCts.IsCancellationRequested)
                    {
                        await MainThread.InvokeOnMainThreadAsync(async () =>
                        {
                            bar.Progress = 0;
                            await bar.ProgressTo(1, 900, Easing.Linear);
                        });
                    }
                }
                catch { /* cancel */ }
            });
        }
        else
        {
            _progressCts?.Cancel();
            try { await bar.ProgressTo(1, 200, Easing.CubicOut); } catch { }
            bar.IsVisible = false;
            bar.Progress = 0;
        }
    }

    // ---------------- Eventos UI ----------------
    private async void RefreshView_Refreshing(object sender, EventArgs e)
    {
        await LoadAsync(refresh: true);
        if (sender is RefreshView rv) rv.IsRefreshing = false;
    }

    private async void Buscar_Clicked(object sender, EventArgs e) => await BuscarAsync();

    private async void SearchBar_SearchButtonPressed(object sender, EventArgs e) => await BuscarAsync();

    private async Task BuscarAsync()
    {
        var sb = Q<SearchBar>("txtBusqueda");
        var q = sb?.Text?.Trim();

        if (string.IsNullOrWhiteSpace(q) || q.Length < 3)
        {
            await DisplayAlert("Aviso", "Ingresá al menos 3 caracteres para buscar.", "OK");
            return;
        }

        _queryActual = q;
        await LoadAsync(refresh: true);
    }

    private async void Reintentar_Clicked(object sender, EventArgs e) => await LoadAsync(refresh: true);

    private async void NoticiasView_RemainingItemsThresholdReached(object sender, EventArgs e)
    {
        if (_nextPageToken is null || _isLoading) return; // no más páginas o ya cargando
        await LoadAsync(refresh: false);
    }

    // (Podés dejar este handler aunque el XAML ya no lo use)
    private async void NoticiasView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            if (sender is CollectionView cv) cv.SelectedItem = null;

            var item = e.CurrentSelection?.FirstOrDefault() as NoticiaItem;
            if (item == null) return;

            if (!TryNormalizeUrl(item.Enlace, out var uri) || uri == null) return;

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                if (await Launcher.Default.CanOpenAsync(uri))
                    await Launcher.Default.OpenAsync(uri);
                else
                    await Launcher.Default.OpenAsync(uri.ToString());
            });
        }
        catch (Exception ex)
        {
            await DisplayAlert("No se pudo abrir el enlace", ex.Message, "OK");
        }
    }

    // ---------------- Carga principal ----------------
    private async Task LoadAsync(bool refresh)
    {
        if (_isLoading) return;
        if (!refresh && _nextPageToken is null) return; // scroll sin token => fin

        _isLoading = true;

        _cts?.Cancel();
        _cts = new CancellationTokenSource();

        try
        {
            await SetLoading(true);

            if (refresh)
            {
                _nextPageToken = null; // reiniciar paginación
                Items.Clear();
            }

            var baseUrl = $"https://newsdata.io/api/1/news?apikey={API_KEY}&language=es&country=uy&q={Uri.EscapeDataString(_queryActual)}";
            var url = _nextPageToken is null ? baseUrl : $"{baseUrl}&page={_nextPageToken}";

            var jsonStr = await http.GetStringAsync(url, _cts.Token);
            var json = JObject.Parse(jsonStr);

            var results = json["results"] as JArray;
            if (results == null || results.Count == 0)
            {
                if (Items.Count == 0) EmptyMessage = "No se encontraron noticias para esa búsqueda.";
                return;
            }

            var existentes = new HashSet<string>(Items.Select(i => i.Enlace ?? ""), StringComparer.OrdinalIgnoreCase);

            var nuevos = results.Select(n => new NoticiaItem
            {
                Titulo = SafeDecode(n["title"]?.ToString(), "Sin título"),
                Descripcion = SafeDecode(n["description"]?.ToString(), "Sin descripción"),
                Enlace = n["link"]?.ToString() ?? "",
                ImagenUrl = n["image_url"]?.ToString() ?? "",
                Fuente = n["source_id"]?.ToString() ?? "",
                FechaPub = ParseDate(n["pubDate"]?.ToString())
            })
            .Where(it => !string.IsNullOrWhiteSpace(it.Enlace) && !existentes.Contains(it.Enlace))
            .ToList();

            foreach (var it in nuevos) Items.Add(it);

            if (refresh)
            {
                GuardarCacheEnArchivo(jsonStr);
                Preferences.Set(LAST_UPDATE_KEY, DateTime.Now);

                var lbl = Q<Label>("lblUltima");
                if (lbl != null)
                    lbl.Text = $"Actualizado: {DateTime.Now.ToString("dd/MM/yyyy HH:mm", uy)}";
            }

            var next = json["nextPage"]?.ToString();
            _nextPageToken = string.IsNullOrWhiteSpace(next) ? null : next;
        }
        catch (TaskCanceledException) { }
        catch (HttpRequestException ex)
        {
            if (Items.Count == 0) EmptyMessage = "Error de red. Intentá nuevamente.";
            await DisplayAlert("Error al cargar noticias", ex.Message, "OK");
        }
        catch (Exception ex)
        {
            if (Items.Count == 0) EmptyMessage = "Error al cargar. Intentá nuevamente.";
            await DisplayAlert("Error al cargar noticias", ex.Message, "OK");
        }
        finally
        {
            await SetLoading(false);
            _isLoading = false;
        }
    }

    // ---------------- Cache archivo ----------------
    private void GuardarCacheEnArchivo(string json)
    {
        try { File.WriteAllText(CacheFilePath, json); }
        catch { /* sin cache si falla */ }
    }

    private void CargarDesdeCacheSiHay()
    {
        try
        {
            if (!File.Exists(CacheFilePath)) return;

            var cache = File.ReadAllText(CacheFilePath);
            if (string.IsNullOrWhiteSpace(cache)) return;

            var json = JObject.Parse(cache);
            var results = json["results"] as JArray;
            if (results == null) return;

            Items.Clear();
            foreach (var n in results)
            {
                Items.Add(new NoticiaItem
                {
                    Titulo = SafeDecode(n["title"]?.ToString(), "Sin título"),
                    Descripcion = SafeDecode(n["description"]?.ToString(), "Sin descripción"),
                    Enlace = n["link"]?.ToString() ?? "",
                    ImagenUrl = n["image_url"]?.ToString() ?? "",
                    Fuente = n["source_id"]?.ToString() ?? "",
                    FechaPub = ParseDate(n["pubDate"]?.ToString())
                });
            }

            var ts = Preferences.Get(LAST_UPDATE_KEY, DateTime.MinValue);
            var lbl = Q<Label>("lblUltima");
            if (ts != DateTime.MinValue && lbl != null)
                lbl.Text = $"Actualizado: {ts.ToString("dd/MM/yyyy HH:mm", uy)}";
        }
        catch { /* ignorar cache roto */ }
    }

    // ---------------- Utilidades ----------------
    private static DateTime? ParseDate(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        if (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var d))
            return d.ToLocalTime();
        return null;
    }

    private static string SafeDecode(string? s, string fallback)
    {
        if (string.IsNullOrWhiteSpace(s)) return fallback;
        try { return WebUtility.HtmlDecode(s); } catch { return s; }
    }

    private static bool TryNormalizeUrl(string? raw, out Uri? uri)
    {
        uri = null;
        if (string.IsNullOrWhiteSpace(raw)) return false;

        var s = raw.Trim();

        if (!s.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !s.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            s = "https://" + s;
        }

        s = WebUtility.HtmlDecode(s);

        return Uri.TryCreate(s, UriKind.Absolute, out uri);
    }

}
