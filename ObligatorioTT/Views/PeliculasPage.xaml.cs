using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel;              // <-- NUEVO
using System.Runtime.CompilerServices;    // <-- NUEVO
using Microsoft.Maui.ApplicationModel;    // Launcher, MainThread
using Microsoft.Maui.Controls;            // ContentPage, ProgressBar, Label, Picker
using Newtonsoft.Json.Linq;

namespace ObligatorioTT.Views;

public partial class PeliculasPage : ContentPage
{
    // BASE con barra final para que las rutas relativas funcionen
    private const string BASE_URL = "https://api.themoviedb.org/3/";
    private const string BASE_IMAGE_URL = "https://image.tmdb.org/t/p/w500";
    // No hardcodear en producción: usar SecureStorage / variables de entorno
    private const string BEARER_TOKEN = "eyJhbGciOiJIUzI1NiJ9.eyJhdWQiOiIwYWVmY2VhM2ZmNzlkMGE4MWE2NTMyOTYxYWQwZmRkMiIsIm5iZiI6MTc1NDUyNDYyMy4yMzcsInN1YiI6IjY4OTNlYmNmYWQ2ZDFlNjc3MzlkYjAwMiIsInNjb3BlcyI6WyJhcGlfcmVhZCJdLCJ2ZXJzaW9uIjoxfQ.pTlF_mHUgRpMeusHn9F0CV70Ff8usYCb5ZCzvrmsu7U";

    private static readonly HttpClient _http = CrearHttpClient();
    private readonly CultureInfo _uy = new("es-UY");

    private bool _cargandoInicial = true;
    private bool _isLoading = false;
    private CancellationTokenSource? _cts;
    private CancellationTokenSource? _progressCts;

    public ObservableCollection<Pelicula> Peliculas { get; } = new();
    public Dictionary<string, int> Generos { get; } = new();

    public class Pelicula : INotifyPropertyChanged
    {
        public string Titulo { get; set; } = "";
        public string Descripcion { get; set; } = "";
        public string PosterUrl { get; set; } = "";
        public string TrailerUrl { get; set; } = "";
        public DateTime? FechaEstreno { get; set; }
        public double Rating { get; set; }
        public bool HasTrailer => !string.IsNullOrWhiteSpace(TrailerUrl);

        // NUEVO: bandera para expandir/colapsar la sinopsis
        private bool _isExpanded;
        public bool IsExpanded
        {
            get => _isExpanded;
            set { if (_isExpanded != value) { _isExpanded = value; OnPropertyChanged(); } }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public PeliculasPage()
    {
        InitializeComponent();
        BindingContext = this;
        _ = CargarGenerosAsync();
    }

    private static HttpClient CrearHttpClient()
    {
        var client = new HttpClient
        {
            BaseAddress = new Uri(BASE_URL),
            Timeout = TimeSpan.FromSeconds(12)
        };
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", BEARER_TOKEN);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return client;
    }

    // UI helpers 
    private async Task SetLoading(bool on)
    {
        var bar = this.FindByName<ProgressBar>("busyBar");
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
                catch { }
            });
        }
        else
        {
            _progressCts?.Cancel();
            try { await bar.ProgressTo(1, 180, Easing.CubicOut); } catch { }
            bar.IsVisible = false;
            bar.Progress = 0;
        }
    }

    private async void RefreshView_Refreshing(object sender, EventArgs e)
    {
        await CargarPeliculasAsync();
        if (sender is RefreshView rv) rv.IsRefreshing = false;
    }

    private async void Reintentar_Clicked(object sender, EventArgs e) => await CargarPeliculasAsync();

    private async Task CargarGenerosAsync()
    {
        try
        {
            await SetLoading(true);

            var resp = await _http.GetStringAsync("genre/movie/list?language=es-ES");
            var json = JObject.Parse(resp);

            Generos.Clear();
            foreach (var g in json["genres"] ?? Enumerable.Empty<JToken>())
            {
                string nombre = g["name"]?.ToString() ?? "";
                int id = g["id"]?.Value<int>() ?? 0;
                if (!string.IsNullOrWhiteSpace(nombre) && id != 0)
                    Generos[nombre] = id;
            }

            var picker = this.FindByName<Picker>("pickerGeneros");
            if (picker != null)
            {
                picker.ItemsSource = Generos.Keys.ToList();
                if (picker.ItemsSource?.Count > 0)
                    picker.SelectedIndex = 0;
            }

            await CargarPeliculasAsync();
            _cargandoInicial = false;

            var lbl = this.FindByName<Label>("lblUpdated");
            if (lbl != null)
                lbl.Text = $"Actualizado: {DateTime.Now.ToString("dd/MM/yyyy HH:mm", _uy)}";
        }
        catch (Exception ex)
        {
            await DisplayAlert("Películas", "No se pudieron cargar géneros.\n" + ex.Message, "OK");
        }
        finally
        {
            await SetLoading(false);
        }
    }

    private async Task CargarPeliculasAsync()
    {
        if (_isLoading) return;
        _isLoading = true;

        _cts?.Cancel();
        _cts = new CancellationTokenSource();

        try
        {
            await SetLoading(true);

            Peliculas.Clear();

            var picker = this.FindByName<Picker>("pickerGeneros");
            if (picker?.SelectedItem is not string nombreGenero || !Generos.TryGetValue(nombreGenero, out int idGenero))
                return;

            var resp = await _http.GetStringAsync("movie/upcoming?include_adult=false&language=es-ES&page=1", _cts.Token);
            var json = JObject.Parse(resp);

            var peliculasJson = (json["results"] as JArray) ?? new JArray();

            var filtradas = peliculasJson
                .Where(p => p["genre_ids"]?.Any(g => g.Value<int>() == idGenero) == true)
                .GroupBy(p => (int)p["id"])  // dedupe
                .Select(g => g.First())
                .Take(8)
                .ToList();

            foreach (var p in filtradas)
            {
                int id = p["id"]?.Value<int>() ?? 0;
                string titulo = p["title"]?.ToString() ?? "Sin título";
                string descripcion = p["overview"]?.ToString() ?? "Sin descripción";
                string? poster = p["poster_path"]?.ToString();
                string posterUrl = !string.IsNullOrWhiteSpace(poster) ? $"{BASE_IMAGE_URL}{poster}" : "";

                DateTime? estreno = null;
                var dateStr = p["release_date"]?.ToString();
                if (DateTime.TryParse(dateStr, out var d)) estreno = d;

                double rating = p["vote_average"]?.Value<double?>() ?? 0;

                string trailerUrl = await ObtenerTrailerAsync(id, _cts.Token);

                Peliculas.Add(new Pelicula
                {
                    Titulo = titulo,
                    Descripcion = descripcion,
                    PosterUrl = posterUrl,
                    TrailerUrl = trailerUrl,
                    FechaEstreno = estreno,
                    Rating = rating
                });
            }

            var lbl = this.FindByName<Label>("lblUpdated");
            if (lbl != null)
                lbl.Text = $"Actualizado: {DateTime.Now.ToString("dd/MM/yyyy HH:mm", _uy)}";
        }
        catch (TaskCanceledException) { }
        catch (HttpRequestException ex)
        {
            await DisplayAlert("Películas", "Error de red.\n" + ex.Message, "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Películas", "No se pudieron cargar estrenos.\n" + ex.Message, "OK");
        }
        finally
        {
            await SetLoading(false);
            _isLoading = false;
        }
    }

    private async Task<string> ObtenerTrailerAsync(int movieId, CancellationToken ct)
    {
        try
        {
            // ES primero
            var es = await _http.GetStringAsync($"movie/{movieId}/videos?language=es-ES", ct);
            var jsonEs = JObject.Parse(es);
            var tEs = (jsonEs["results"] as JArray)?.FirstOrDefault(t =>
                (string?)t?["type"] == "Trailer" && (string?)t?["site"] == "YouTube" && !string.IsNullOrWhiteSpace((string?)t?["key"]));
            if (tEs != null) return $"https://www.youtube.com/watch?v={tEs["key"]}";

            // EN fallback
            var en = await _http.GetStringAsync($"movie/{movieId}/videos?language=en-US", ct);
            var jsonEn = JObject.Parse(en);
            var tEn = (jsonEn["results"] as JArray)?.FirstOrDefault(t =>
                (string?)t?["type"] == "Trailer" && (string?)t?["site"] == "YouTube" && !string.IsNullOrWhiteSpace((string?)t?["key"]));
            if (tEn != null) return $"https://www.youtube.com/watch?v={tEn["key"]}";
        }
        catch { }
        return "";
    }

    // ---------- Eventos ----------
    private async void pickerGeneros_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (_cargandoInicial) return;
        await CargarPeliculasAsync();
    }

    private async void VerTrailer_Clicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is Pelicula p && !string.IsNullOrWhiteSpace(p.TrailerUrl))
        {
            // Navega a la nueva página que creamos (TrailerPage)
            await Navigation.PushAsync(new TrailerPage(p.TrailerUrl));
        }
    }


    // NUEVO: expandir/colapsar sinopsis en la misma card
    private void ToggleExpand_Clicked(object sender, EventArgs e)
    {
        if (sender is Element el && el.BindingContext is Pelicula p)
            p.IsExpanded = !p.IsExpanded;
    }

}
