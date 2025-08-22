using System;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;
using Microsoft.Maui.Media;
using Microsoft.Maui.Controls;
using ObligatorioTT.Data;
using ObligatorioTT.Models;
using ObligatorioTT.Helpers;

namespace ObligatorioTT.Views;

public partial class PerfilPage : ContentPage
{
    private readonly DatabaseService _db;
    private Usuario? _usuarioActual;
    private string _fotoLocalPath = string.Empty;

    public PerfilPage()
    {
        InitializeComponent();
        _db = ServiceHelper.GetService<DatabaseService>(); // reutilizamos tu helper
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CargarPerfilAsync();

        // Limpia el flag SOLO cuando ya volvimos a la página
        if (Preferences.ContainsKey("SkipLogoutOnce"))
            Preferences.Remove("SkipLogoutOnce");
    }

    private async Task CargarPerfilAsync()
    {
        var userId = Preferences.Get("LoggedUserId", 0);

        // Fallback: si no hay Id pero sí nombre de usuario, lo recuperamos y re-sincronizamos el Id
        if (userId == 0)
        {
            var userName = Preferences.Get("LoggedUser", string.Empty);
            if (!string.IsNullOrEmpty(userName))
            {
                var u = await _db.GetUsuarioByUserAsync(userName);
                if (u != null)
                {
                    Preferences.Set("LoggedUserId", u.Id);
                    userId = u.Id;
                }
            }
        }

        if (userId == 0)
        {
            await DisplayAlert("Perfil", "No hay usuario logueado.", "OK");
            return;
        }

        var lista = await _db.GetUsuariosAsync();
        _usuarioActual = lista.FirstOrDefault(u => u.Id == userId);
        if (_usuarioActual == null)
        {
            await DisplayAlert("Perfil", "Usuario no encontrado.", "OK");
            return;
        }

        txtNombre.Text = _usuarioActual.NombreCompleto;
        txtDir.Text = _usuarioActual.Direccion;
        txtTel.Text = _usuarioActual.Telefono;
        txtEmail.Text = _usuarioActual.Email;
        _fotoLocalPath = _usuarioActual.FotoPath ?? string.Empty;
        imgFoto.Source = _fotoLocalPath;
    }

    private async void BtnElegirFoto_Clicked(object sender, EventArgs e)
    {
        try
        {
            // Evita que el ciclo de vida (Activity recreate) dispare el "cierre de sesión" una sola vez
            Preferences.Set("SkipLogoutOnce", true);

            var photo = await MediaPicker.PickPhotoAsync();
            if (photo != null)
            {
                _fotoLocalPath = await GuardarFotoLocalAsync(photo);
                imgFoto.Source = _fotoLocalPath;
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Foto", $"No se pudo seleccionar la foto: {ex.Message}", "OK");
        }
        // IMPORTANTE: ya NO removemos el flag acá.
        // Se limpia en OnAppearing, cuando la página vuelve al frente.
    }

    private async Task<string> GuardarFotoLocalAsync(FileResult photo)
    {
        var fileName = $"usr_{DateTime.UtcNow.Ticks}.jpg";
        var dest = Path.Combine(FileSystem.AppDataDirectory, fileName);
        using var src = await photo.OpenReadAsync();
        using var dst = File.OpenWrite(dest);
        await src.CopyToAsync(dst);
        return dest;
    }

    private static bool EsEmailValido(string email)
    {
        try { _ = new MailAddress(email); return true; }
        catch { return false; }
    }

    private static bool EsTelefonoValido(string tel)
    {
        var onlyDigits = new string(tel.Where(char.IsDigit).ToArray());
        return !string.IsNullOrEmpty(onlyDigits)
               && onlyDigits.Length >= 7
               && onlyDigits.Length <= 20
               && onlyDigits.Length == tel.Length;
    }

    private async void BtnGuardar_Clicked(object sender, EventArgs e)
    {
        if (_usuarioActual == null)
        {
            await DisplayAlert("Perfil", "No hay datos cargados.", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(txtNombre.Text) ||
            string.IsNullOrWhiteSpace(txtDir.Text) ||
            string.IsNullOrWhiteSpace(txtTel.Text) ||
            string.IsNullOrWhiteSpace(txtEmail.Text))
        {
            await DisplayAlert("Validación", "Todos los campos son obligatorios.", "OK");
            return;
        }

        if (!EsTelefonoValido(txtTel.Text!))
        {
            await DisplayAlert("Validación", "Teléfono inválido.", "OK");
            return;
        }

        if (!EsEmailValido(txtEmail.Text!))
        {
            await DisplayAlert("Validación", "Email inválido.", "OK");
            return;
        }

        // Actualizar modelo
        _usuarioActual.NombreCompleto = txtNombre.Text!.Trim();
        _usuarioActual.Direccion = txtDir.Text!.Trim();
        _usuarioActual.Telefono = txtTel.Text!.Trim();
        _usuarioActual.Email = txtEmail.Text!.Trim();
        _usuarioActual.FotoPath = _fotoLocalPath;

        var (ok, error) = await _db.ActualizarUsuarioAsync(_usuarioActual);
        if (!ok)
        {
            await DisplayAlert("Perfil", error ?? "No se pudo actualizar.", "OK");
            return;
        }

        await DisplayAlert("Perfil", "Datos actualizados.", "OK");
        await Shell.Current.GoToAsync("//MainPage");
    }
}
    