using Newtonsoft.Json.Linq;
using System.Net.Http;
using Microsoft.Maui.Controls;
using System.Collections.ObjectModel;
using System.Net.Http.Headers;

namespace ObligatorioTT.Views;

public partial class PeliculasPage : ContentPage
{
    private const string BASE_URL = "https://api.themoviedb.org/3";
    private const string BASE_IMAGE_URL = "https://image.tmdb.org/t/p/w500";
    private const string BEARER_TOKEN = "eyJhbGciOiJIUzI1NiJ9.eyJhdWQiOiIwYWVmY2VhM2ZmNzlkMGE4MWE2NTMyOTYxYWQwZmRkMiIsIm5iZiI6MTc1NDUyNDYyMy4yMzcsInN1YiI6IjY4OTNlYmNmYWQ2ZDFlNjc3MzlkYjAwMiIsInNjb3BlcyI6WyJhcGlfcmVhZCJdLCJ2ZXJzaW9uIjoxfQ.pTlF_mHUgRpMeusHn9F0CV70Ff8usYCb5ZCzvrmsu7U";

    private bool _cargandoInicial = true;

    public ObservableCollection<Pelicula> Peliculas { get; set; } = new();
    public Dictionary<string, int> Generos { get; set; } = new();

    public class Pelicula
    {
        public string Titulo { get; set; }
        public string Descripcion { get; set; }
        public string PosterUrl { get; set; }
        public string TrailerUrl { get; set; }
    }

    public PeliculasPage()
    {
        InitializeComponent();
        BindingContext = this;
        _ = CargarGeneros();
    }

    private HttpClient CrearCliente()
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", BEARER_TOKEN);
        return client;
    }
    
    private async Task CargarGeneros()
    {
        using var client = CrearCliente();
        var url = $"{BASE_URL}/genre/movie/list?language=es-ES";
        var response = await client.GetStringAsync(url);
        var json = JObject.Parse(response);

        foreach (var genero in json["genres"])
        {
            string nombre = genero["name"].ToString();
            int id = genero["id"].Value<int>();
            Generos[nombre] = id;
        }

        pickerGeneros.ItemsSource = Generos.Keys.ToList();
        pickerGeneros.SelectedIndex = 0;

        await CargarPeliculas();

        _cargandoInicial = false;
    }

    private async Task CargarPeliculas()
    {
        Peliculas.Clear();

        if (pickerGeneros.SelectedItem == null)
            return;

        int idGenero = Generos[pickerGeneros.SelectedItem.ToString()];
        using var client = CrearCliente();
        var url = $"{BASE_URL}/movie/upcoming?include_adult=false&language=es-ES&&page=1";
        var response = await client.GetStringAsync(url);
        var json = JObject.Parse(response);

        var peliculasJson = json["results"]
            .Where(p => p["genre_ids"].Any(g => g.Value<int>() == idGenero))
            .Take(5);
        var peliculasUnicas = peliculasJson
            .GroupBy(p => (string)p["id"])
            .Select(g => g.First())
            .Take(5);

        foreach (var p in peliculasUnicas)
        {
            string id = p["id"].ToString();
            string titulo = p["title"]?.ToString() ?? "Sin título";
            string descripcion = p["overview"]?.ToString() ?? "Sin descripción";
            string poster = p["poster_path"]?.ToString();
            string posterUrl = poster != null ? $"{BASE_IMAGE_URL}{poster}" : "";

            string trailerUrl = "";
            var trailerResponse = await client.GetStringAsync($"{BASE_URL}/movie/{id}/videos?language=e-US");
            var trailerJson = JObject.Parse(trailerResponse);
            var trailer = trailerJson["results"]?.FirstOrDefault(t =>
                t["type"]?.ToString() == "Trailer" &&
                t["site"]?.ToString() == "YouTube");

            if (trailer != null && !string.IsNullOrWhiteSpace(descripcion))
                trailerUrl = $"https://www.youtube.com/watch?v={trailer["key"]}";

            Peliculas.Add(new Pelicula
            {
                Titulo = titulo,
                Descripcion = descripcion,
                PosterUrl = posterUrl,
                TrailerUrl = trailerUrl
            });
        }
    }

    private async void pickerGeneros_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (_cargandoInicial) return;

        await CargarPeliculas();
    }

    private async void TapGestureRecognizer_Tapped(object sender, EventArgs e)
    {
        if (sender is Label label && label.BindingContext is Pelicula pelicula)
        {
            if (!string.IsNullOrEmpty(pelicula.TrailerUrl))
                await Launcher.Default.OpenAsync(pelicula.TrailerUrl);
        }
    }
}
