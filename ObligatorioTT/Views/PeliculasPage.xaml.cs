using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;                             // LINQ
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Maui.ApplicationModel;        // Launcher
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;                // Color, brushes

using Newtonsoft.Json.Linq;

namespace ObligatorioTT.Views;

public partial class PeliculasPage : ContentPage
{
    private const string BASE_URL = "https://api.themoviedb.org/3";
    private const string BASE_IMAGE_URL = "https://image.tmdb.org/t/p/w500";
    private const string LANG_ES = "es-ES";
    private const string LANG_EN = "en-US";

    // ? Podés usar SecureStorage; dejo tu token si ya lo tenías.
    private const string BEARER_TOKEN =
        "eyJhbGciOiJIUzI1NiJ9.eyJhdWQiOiIwYWVmY2VhM2ZmNzlkMGE4MWE2NTMyOTYxYWQwZmRkMiIsIm5iZiI6MTc1NDUyNDYyMy4yMzcsInN1YiI6IjY4OTNlYmNmYWQ2ZDFlNjc3MzlkYjAwMiIsInNjb3BlcyI6WyJhcGlfcmVhZCJdLCJ2ZXJzaW9uIjoxfQ.pTlF_mHUgRpMeusHn9F0CV70Ff8usYCb5ZCzvrmsu7U";

    private readonly HttpClient _http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };

    private bool _cargandoInicial = true;
    private CancellationTokenSource? _cts;

    public ObservableCollection<Pelicula> Peliculas { get; } = new();
    public Dictionary<string, int> Generos { get; } = new();

    public class Pelicula
    {
        public string Titulo { get; set; } = "";
        public string Descripcion { get; set; } = "";
        public string PosterUrl { get; set; } = "";
        public string TrailerUrl { get; set; } = "";
        public string Genero { get; set; } = "";
        public string FechaLanzamiento { get; set; } = ""; // ? para el detalle
    }

    public PeliculasPage()
    {
        InitializeComponent();
        BindingContext = this;

        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", BEARER_TOKEN);
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        _ = CargarGenerosAsync();
    }

    protected override void OnDisappearing()
    {
        _cts?.Cancel();
        base.OnDisappearing();
    }

    // ---------------- HTTP helper ----------------
    private async Task<JObject> GetJsonAsync(string url, CancellationToken ct)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        using var res = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
        res.EnsureSuccessStatusCode();
        var str = await res.Content.ReadAsStringAsync(ct);
        return JObject.Parse(str);
    }

    // ---------------- Data: géneros ----------------
    private async Task CargarGenerosAsync()
    {
        try
        {
            var url = $"{BASE_URL}/genre/movie/list?language={LANG_ES}";
            var json = await GetJsonAsync(url, CancellationToken.None);

            Generos.Clear();
            Generos["Todos"] = 0;

            var arr = json["genres"] as JArray;
            if (arr != null)
            {
                foreach (var genero in arr)
                {
                    string nombre = genero["name"]?.ToString() ?? "Desconocido";
                    int id = genero["id"]?.Value<int>() ?? 0;
                    if (id != 0) Generos[nombre] = id;
                }
            }

            pickerGeneros.ItemsSource = Generos.Keys.ToList();
            pickerGeneros.SelectedIndex = 0;

            await CargarPeliculasAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Géneros", $"No se pudieron cargar los géneros.\n{ex.Message}", "OK");
        }
        finally
        {
            _cargandoInicial = false;
        }
    }

    // ---------------- Data: películas ----------------
    private async Task CargarPeliculasAsync()
    {
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        var ct = _cts.Token;

        try
        {
            Peliculas.Clear();
            if (pickerGeneros.SelectedItem == null) return;

            int idGenero = Generos[pickerGeneros.SelectedItem.ToString()!];

            // Upcoming (varias páginas)
            var upcoming = new List<JToken>();
            for (int page = 1; page <= 3; page++)
            {
                var url = $"{BASE_URL}/movie/upcoming?include_adult=false&language={LANG_ES}&page={page}";
                var json = await GetJsonAsync(url, ct);
                var results = (json["results"] as JArray) ?? new JArray();
                upcoming.AddRange(results);
            }

            var candidatos = upcoming
                .GroupBy(p => (int)p["id"]!)
                .Select(g => g.First())
                .ToList();

            var seleccion = (idGenero == 0)
                ? candidatos
                : candidatos.Where(p => p["genre_ids"]!.Any(g => g!.Value<int>() == idGenero)).ToList();

            if (idGenero != 0 && seleccion.Count < 5)
            {
                var extra = await BuscarPorGeneroAsync(idGenero, ct);
                var ids = new HashSet<int>(seleccion.Select(p => (int)p["id"]!));
                foreach (var d in extra)
                    if (ids.Add((int)d["id"]!)) seleccion.Add(d);
            }

            if (idGenero == 0 && seleccion.Count == 0)
            {
                var trendUrl = $"{BASE_URL}/trending/movie/week?language={LANG_ES}";
                var trendJson = await GetJsonAsync(trendUrl, ct);
                var trend = (trendJson["results"] as JArray) ?? new JArray();
                seleccion = trend.ToList();
            }

            seleccion = seleccion
                .GroupBy(p => (int)p["id"]!)
                .Select(g => g.First())
                .Take(5)
                .ToList();

            if (seleccion.Count == 0)
            {
                await DisplayAlert("Sin resultados", "No hay películas para este género ahora mismo.", "OK");
                return;
            }

            foreach (var p in seleccion)
            {
                ct.ThrowIfCancellationRequested();

                int id = p["id"]?.Value<int>() ?? 0;
                string titulo = p["title"]?.ToString() ?? "Sin título";
                string descripcion = p["overview"]?.ToString() ?? "Sin descripción";
                string poster = p["poster_path"]?.ToString() ?? "";
                string posterUrl = string.IsNullOrWhiteSpace(poster) ? "" : $"{BASE_IMAGE_URL}{poster}";

                string fechaRaw = p["release_date"]?.ToString() ?? "";
                string fechaFmt = FormatearFecha(fechaRaw);

                string trailerUrl = id != 0 ? await BuscarTrailerAsync(id, ct) : "";

                Peliculas.Add(new Pelicula
                {
                    Titulo = titulo,
                    Descripcion = descripcion,
                    PosterUrl = posterUrl,
                    TrailerUrl = trailerUrl,
                    FechaLanzamiento = fechaFmt
                });
            }

            await AnimateInAsync();
        }
        catch (OperationCanceledException)
        {
            // usuario cambió género / salió
        }
        catch (Exception ex)
        {
            await DisplayAlert("Películas", $"No se pudieron cargar las películas.\n{ex.Message}", "OK");
        }
    }

    private async Task<List<JToken>> BuscarPorGeneroAsync(int idGenero, CancellationToken ct)
    {
        var hoy = DateTime.UtcNow.ToString("yyyy-MM-dd");

        var url = $"{BASE_URL}/discover/movie" +
                  $"?language={LANG_ES}" +
                  $"&with_genres={idGenero}" +
                  $"&include_adult=false" +
                  $"&include_video=false" +
                  $"&sort_by=primary_release_date.asc" +
                  $"&primary_release_date.gte={hoy}" +
                  $"&page=1";

        var json = await GetJsonAsync(url, ct);
        var results = (json["results"] as JArray) ?? new JArray();
        return results.Take(20).ToList();
    }

    private async Task<string> BuscarTrailerAsync(int movieId, CancellationToken ct)
    {
        var esUrl = $"{BASE_URL}/movie/{movieId}/videos?language={LANG_ES}";
        var es = await GetJsonAsync(esUrl, ct);
        var trailerEs = es["results"]?.FirstOrDefault(t =>
            t?["type"]?.ToString() == "Trailer" &&
            t?["site"]?.ToString() == "YouTube");
        if (trailerEs != null)
            return $"https://www.youtube.com/watch?v={trailerEs["key"]}";

        var enUrl = $"{BASE_URL}/movie/{movieId}/videos?language={LANG_EN}";
        var en = await GetJsonAsync(enUrl, ct);
        var trailerEn = en["results"]?.FirstOrDefault(t =>
            t?["type"]?.ToString() == "Trailer" &&
            t?["site"]?.ToString() == "YouTube");
        if (trailerEn != null)
            return $"https://www.youtube.com/watch?v={trailerEn["key"]}";

        return "";
    }

    // ---------------- UI events ----------------
    private async void pickerGeneros_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (_cargandoInicial) return;
        await CargarPeliculasAsync();
    }

    // ? Ver tráiler (abre YouTube)
    private async void OnPlayButtonClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.BindingContext is Pelicula p)
        {
            if (!string.IsNullOrWhiteSpace(p.TrailerUrl))
            {
                try { await Launcher.Default.OpenAsync(p.TrailerUrl); }
                catch { await DisplayAlert("Tráiler", "No se pudo abrir el enlace.", "OK"); }
            }
            else
            {
                await DisplayAlert("Tráiler", "No hay tráiler disponible.", "OK");
            }
        }
    }

    // Tap en la card: animación + mostrar detalle (overlay)
    private async void OnCardTapped(object sender, TappedEventArgs e)
    {
        if (sender is VisualElement v)
        {
            await v.ScaleTo(0.97, 60, Easing.CubicIn);
            await v.TranslateTo(0, -2, 60, Easing.CubicIn);
            await Task.WhenAll(
                v.ScaleTo(1.0, 120, Easing.CubicOut),
                v.TranslateTo(0, 0, 120, Easing.CubicOut)
            );
        }

        if (sender is BindableObject bo && bo.BindingContext is Pelicula p)
        {
            await ShowDetailsAsync(p);
        }
    }

    // ---------- Overlay Detalle ----------
    private async Task ShowDetailsAsync(Pelicula p)
    {
        // Póster / placeholder
        if (string.IsNullOrWhiteSpace(p.PosterUrl))
        {
            DetailsPoster.Source = null;
            DetailsPoster.Background = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(1, 1),
                GradientStops = new GradientStopCollection
                {
                    new GradientStop(Color.FromArgb("#1B2940"), 0f),
                    new GradientStop(Color.FromArgb("#0F1A2A"), 1f),
                }
            };
        }
        else
        {
            DetailsPoster.Background = null;
            DetailsPoster.Source = p.PosterUrl;
        }

        DetailsTitle.Text = p.Titulo;
        DetailsDate.Text = string.IsNullOrWhiteSpace(p.FechaLanzamiento) ? "—" : p.FechaLanzamiento;
        DetailsOverview.Text = string.IsNullOrWhiteSpace(p.Descripcion) ? "Sinopsis no disponible." : p.Descripcion;

        DetailsOverlay.IsVisible = true;
        DetailsCard.Opacity = 0;
        await DetailsCard.FadeTo(1, 180, Easing.CubicOut);
    }

    private void CloseDetails(object sender, EventArgs e)
    {
        DetailsOverlay.IsVisible = false;
        DetailsPoster.Source = null; // liberar
    }

    // ---------- Util ----------
    private static string FormatearFecha(string yyyyMMdd)
    {
        return DateTime.TryParse(yyyyMMdd, out var dt)
            ? dt.ToString("dd/MM/yyyy")
            : (string.IsNullOrWhiteSpace(yyyyMMdd) ? "—" : yyyyMMdd);
    }

    // ---------- Animación de entrada ----------
    private async Task AnimateInAsync()
    {
        await Task.Delay(30);

        var cells = GetVisibleCells(PeliculasView).ToList();
        foreach (var c in cells)
        {
            c.Opacity = 0;
            c.TranslationY = 10;
        }

        foreach (var c in cells)
        {
            await Task.WhenAll(
                c.FadeTo(1, 220, Easing.CubicOut),
                c.TranslateTo(0, 0, 220, Easing.CubicOut)
            );
            await Task.Delay(35);
        }
    }

    private static IEnumerable<VisualElement> GetVisibleCells(CollectionView cv)
    {
        foreach (var child in cv.LogicalChildren)
            if (child is VisualElement ve)
                yield return ve;
    }
}