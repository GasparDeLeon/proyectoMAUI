using Newtonsoft.Json.Linq;
using System.Net.Http;
using Microsoft.Maui.Storage;
using System.Linq;

namespace ObligatorioTT.Views;

public partial class NoticiasPage : ContentPage
{
    private const string API_KEY = "pub_4b4e4d9e60da4fcb9c8073837a300312";
    private const string LAST_UPDATE_KEY = "UltimaActualizacionNoticias";

    public class Noticia
    {
        public string Titulo { get; set; }
        public string Descripcion { get; set; }
        public string Enlace { get; set; }  // Nuevo campo para el link
    }

    public Command<string> EnlaceCommand { get; }

    public NoticiasPage()
    {
        InitializeComponent();
        EnlaceCommand = new Command<string>(async (url) => await AbrirEnlace(url));
        BindingContext = this;
        _ = CargarNoticiasSiCorresponde();
    }

    private async Task CargarNoticiasSiCorresponde()
    {
        DateTime ultimaActualizacion = Preferences.Get(LAST_UPDATE_KEY, DateTime.MinValue);

        bool necesitaActualizar = (DateTime.Now - ultimaActualizacion).TotalHours >= 24;

        if (necesitaActualizar)
        {
            await CargarNoticias();
            Preferences.Set(LAST_UPDATE_KEY, DateTime.Now);
        }
        else
        {
            await CargarNoticias();
        }
    }

    private async Task CargarNoticias(string palabraClave = "uruguay")
    {
        try
        {
            string url = $"https://newsdata.io/api/1/news?apikey={API_KEY}&language=es&country=uy&q={palabraClave}";

            using HttpClient client = new();
            var response = await client.GetStringAsync(url);
            var json = JObject.Parse(response);

            var noticiasJson = json["results"];

            if (noticiasJson == null || !noticiasJson.Any())
            {
                await DisplayAlert("Aviso", "No se encontraron noticias para esa palabra clave.", "OK");
                NoticiasView.ItemsSource = null;
                return;
            }

            var noticias = noticiasJson
                .Take(5)
                .Select(n => new Noticia
                {
                    Titulo = n["title"]?.ToString() ?? "Sin título",
                    Descripcion = n["description"]?.ToString() ?? "Sin descripción",
                    Enlace = n["link"]?.ToString() ?? ""
                })
                .ToList();

            NoticiasView.ItemsSource = noticias;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error al cargar noticias", ex.Message, "OK");
        }
    }
    private async void Buscar_Clicked(object sender, EventArgs e)
    {
        string palabraClave = txtBusqueda.Text?.Trim();

        if (string.IsNullOrWhiteSpace(palabraClave))
        {
            await DisplayAlert("Aviso", "Ingrese una palabra clave para buscar.", "OK");
            return;
        }

        await CargarNoticias(palabraClave);
    }


    private async Task AbrirEnlace(string url)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(url))
                await Launcher.Default.OpenAsync(url);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"No se pudo abrir el enlace.\n{ex.Message}", "OK");
        }
    }
}
