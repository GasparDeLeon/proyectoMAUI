using Microsoft.Maui.ApplicationModel;           // MainThread
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.PlatformConfiguration.WindowsSpecific;
using Microsoft.Maui.Storage;                    // Preferences, FileSystem
using ObligatorioTT.Data;
using ObligatorioTT.Helpers;
using ObligatorioTT.Models;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ObligatorioTT
{
    public partial class MainPage : ContentPage
    {
        private readonly DatabaseService _db;
        private CultureInfo _fmt;                     // ✅ Cultura segura con fallback
        private bool _isPlaying;
        private CancellationTokenSource? _dummyProgressCts;

        public MainPage()
        {
            InitializeComponent();
            _db = ServiceHelper.GetService<DatabaseService>();

            // ✅ Inicializar cultura de forma segura (sin crashear en Android Release)
            try { _fmt = CultureInfo.GetCultureInfo("es-UY"); }
            catch { _fmt = CultureInfo.CurrentCulture; }

            // Navegación
            tapNoticias.Tapped += (_, __) => Shell.Current.GoToAsync("//NoticiasPage");
            tapClima.Tapped += (_, __) => Shell.Current.GoToAsync("//ClimaPage");
            tapPeliculas.Tapped += (_, __) => Shell.Current.GoToAsync("//PeliculasPage");
            tapPatrocinadores.Tapped += (_, __) => Shell.Current.GoToAsync("//PatrocinadoresPage");
            tapPerfil.Tapped += (_, __) => Shell.Current.GoToAsync("//PerfilPage");
            tapCotizaciones.Tapped += (_, __) => Shell.Current.GoToAsync("//CotizacionesPage");

            // Pull to refresh + mini player
            refreshView.Refreshing += async (_, __) => await RefrescarTodoAsync();
            btnPlayPause.Clicked += OnPlayPauseClicked;

            // Actualización desde Cotizaciones
            MessagingCenter.Subscribe<object, double>(this, "USD_UPDATED", (sender, valor) =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    lblUsdUy.Text = $"$ {valor:N2}";
                    Preferences.Set("UltimoUSD", valor);
                    Preferences.Set("UltimoUSD_TS", DateTime.UtcNow.ToString("o"));
                });
            });
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await ActualizarBienvenidaAsync();
            await CargarAvatarAsync();
            await RefrescarTodoAsync();
        }

        // ============================
        //  Bienvenida (nombre + fecha)
        // ============================
        private async Task ActualizarBienvenidaAsync()
        {
            try
            {
                var usuario = await GetUsuarioLogueadoAsync();
                var nombre = usuario?.NombreCompleto?.Trim();
                lblSaludo.Text = string.IsNullOrWhiteSpace(nombre)
                    ? "¡Hola!"
                    : $"¡Hola, {nombre.Split(' ').First()}!";

                // ✅ Usar cultura segura (_fmt)
                lblFecha.Text = DateTime.Now.ToString("dddd d 'de' MMMM 'de' yyyy", _fmt);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MainPage] ActualizarBienvenidaAsync: {ex}");
                lblSaludo.Text = "¡Hola!";
                lblFecha.Text = DateTime.Now.ToString("d", _fmt);
            }
        }

        private async Task<Usuario?> GetUsuarioLogueadoAsync()
        {
            try
            {
                var userId = Preferences.Get("LoggedUserId", 0);
                if (userId > 0)
                {
                    var lista = await _db.GetUsuariosAsync();
                    return lista.FirstOrDefault(u => u.Id == userId);
                }

                var userName = Preferences.Get("LoggedUser", string.Empty);
                if (!string.IsNullOrWhiteSpace(userName))
                    return await _db.GetUsuarioByUserAsync(userName);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MainPage] GetUsuarioLogueadoAsync: {ex}");
            }
            return null;
        }

        // ============================
        //  Avatar (foto real + fallback)
        // ============================
        private async Task CargarAvatarAsync()
        {
            try
            {
                var usuario = await GetUsuarioLogueadoAsync();
                if (usuario is null)
                {
                    imgAvatar.Source = "avatar_placeholder.png";
                    return;
                }

                var posiblesRutas = new[]
                {
                    usuario.GetType().GetProperty("FotoLocalPath")?.GetValue(usuario) as string,
                    usuario.GetType().GetProperty("FotoPath")?.GetValue(usuario) as string,
                    usuario.GetType().GetProperty("Foto")?.GetValue(usuario) as string
                }
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Select(p => p!.Trim())
                .ToList();

                string? ruta = posiblesRutas.FirstOrDefault(p => File.Exists(p));

                if (ruta is null)
                {
                    var fotoBytesProp = usuario.GetType().GetProperty("FotoBytes");
                    var bytes = fotoBytesProp?.GetValue(usuario) as byte[];
                    if (bytes is { Length: > 0 })
                    {
                        var tmp = Path.Combine(FileSystem.CacheDirectory, $"avatar_{usuario.Id}.png");
                        await File.WriteAllBytesAsync(tmp, bytes);
                        ruta = tmp;
                    }
                }

                imgAvatar.Source = ruta is not null ? ImageSource.FromFile(ruta) : "avatar_placeholder.png";
            }
            catch
            {
                imgAvatar.Source = "avatar_placeholder.png";
            }
        }

        // ============================
        //  Refresh general
        // ============================
        private async Task RefrescarTodoAsync()
        {
            try
            {
                refreshView.IsRefreshing = true;
                await Task.WhenAll(
                    CargarClimaAsync(),
                    MostrarUsdDesdeCacheAsync()
                );
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MainPage] RefrescarTodoAsync: {ex}");
            }
            finally
            {
                refreshView.IsRefreshing = false;
            }
        }

        // ============================
        //  Clima (placeholder)
        // ============================
        private async Task CargarClimaAsync()
        {
            await Task.Delay(100); // simulación I/O
            lblClimaResumen.Text = "Punta del Este";
            // Fallback de ícono
            imgClima.Source = "sunny.png";
        }

        // ============================
        //  Cotización (desde cache/Prefs)
        // ============================
        private Task MostrarUsdDesdeCacheAsync()
        {
            double cache = Preferences.Get("UltimoUSD", 0.0);
            lblUsdUy.Text = cache > 0 ? $"$ {cache:N2}" : "$ --,--";
            return Task.CompletedTask;
        }

        // ============================
        //  Mini Player (placeholder)
        // ============================
        private void OnPlayPauseClicked(object? sender, EventArgs e)
        {
            _isPlaying = !_isPlaying;
            btnPlayPause.Source = _isPlaying ? "ic_pause.png" : "ic_play.png";

            if (_isPlaying)
            {
                lblNowTitle.Text = "Reproduciendo";
                lblNowTrack.Text = "La Voz del Este FM";
                SimularProgreso();
            }
            else
            {
                lblNowTitle.Text = "Pausado";
                CancelarProgresoDummy();
                pbProgreso.Progress = 0;
            }
        }

        private async void SimularProgreso()
        {
            CancelarProgresoDummy();
            _dummyProgressCts = new CancellationTokenSource();
            var token = _dummyProgressCts.Token;

            try
            {
                pbProgreso.Progress = 0;
                while (!token.IsCancellationRequested && _isPlaying)
                {
                    pbProgreso.Progress = Math.Min(1.0, pbProgreso.Progress + 0.02);
                    if (pbProgreso.Progress >= 1.0) pbProgreso.Progress = 0;
                    await Task.Delay(300, token);
                }
            }
            catch (TaskCanceledException) { /* ignore */ }
        }

        private void CancelarProgresoDummy()
        {
            try { _dummyProgressCts?.Cancel(); }
            catch { }
            finally { _dummyProgressCts?.Dispose(); _dummyProgressCts = null; }
        }
    }
}
