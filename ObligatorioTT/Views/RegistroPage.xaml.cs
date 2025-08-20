using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Mail;
using Microsoft.Maui.Storage;
using Microsoft.Maui.Media;
using ObligatorioTT.Data;
using ObligatorioTT.Models;
using ObligatorioTT.Utils;

namespace ObligatorioTT.Views;

public partial class RegistroPage : ContentPage
{
    private readonly DatabaseService _db;
    private string _fotoLocalPath = string.Empty;

    public RegistroPage(DatabaseService db)
    {
        InitializeComponent();
        _db = db;
    }

    private async void BtnTomarFoto_Clicked(object sender, EventArgs e)
    {
        try
        {
            var photo = await MediaPicker.CapturePhotoAsync();
            if (photo != null)
                _fotoLocalPath = await GuardarFotoLocalAsync(photo);
            imgFoto.Source = _fotoLocalPath;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Foto", $"No se pudo tomar la foto: {ex.Message}", "OK");
        }
    }

    private async void BtnElegirFoto_Clicked(object sender, EventArgs e)
    {
        try
        {
            var photo = await MediaPicker.PickPhotoAsync();
            if (photo != null)
                _fotoLocalPath = await GuardarFotoLocalAsync(photo);
            imgFoto.Source = _fotoLocalPath;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Foto", $"No se pudo seleccionar la foto: {ex.Message}", "OK");
        }
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

    // ? Ahora valida que TODOS los campos estén completos (incluye foto)
    private bool Validar()
    {
        if (string.IsNullOrWhiteSpace(txtUser.Text)) return false;
        if (string.IsNullOrWhiteSpace(txtPass.Text)) return false;
        if (string.IsNullOrWhiteSpace(txtNombre.Text)) return false;
        if (string.IsNullOrWhiteSpace(txtDir.Text)) return false;
        if (string.IsNullOrWhiteSpace(txtTel.Text)) return false;
        if (string.IsNullOrWhiteSpace(txtEmail.Text)) return false;
        if (string.IsNullOrWhiteSpace(_fotoLocalPath)) return false;
        return true;
    }

    private static bool EsEmailValido(string email)
    {
        try { _ = new MailAddress(email); return true; }
        catch { return false; }
    }

    private static bool EsTelefonoValido(string tel)
    {
        // Solo dígitos, entre 7 y 20
        var onlyDigits = new string(tel.Where(char.IsDigit).ToArray());
        return !string.IsNullOrEmpty(onlyDigits)
               && onlyDigits.Length >= 7
               && onlyDigits.Length <= 20
               && onlyDigits.Length == tel.Length;
    }

    private async void BtnRegistrar_Clicked(object sender, EventArgs e)
    {
        // 1) Obligatorios
        if (!Validar())
        {
            await DisplayAlert("Validación", "Todos los campos son obligatorios (incluida la foto).", "OK");
            return;
        }

        // 2) Reglas de formato
        if (txtPass.Text!.Length < 6)
        {
            await DisplayAlert("Validación", "La contraseña debe tener al menos 6 caracteres.", "OK");
            return;
        }

        if (!EsEmailValido(txtEmail.Text!))
        {
            await DisplayAlert("Validación", "Ingresá un email válido.", "OK");
            return;
        }

        if (!EsTelefonoValido(txtTel.Text!))
        {
            await DisplayAlert("Validación", "Ingresá un teléfono válido (solo números, 7 a 20 dígitos).", "OK");
            return;
        }

        var userName = txtUser.Text!.Trim();
        var existente = await _db.GetUsuarioByUserAsync(userName);
        if (existente != null)
        {
            await DisplayAlert("Registro", "El nombre de usuario ya existe.", "OK");
            return;
        }

        var nuevo = new Usuario
        {
            UserName = userName,
            Password = SecurityHelper.Sha256(txtPass.Text!),
            NombreCompleto = txtNombre.Text!.Trim(),
            Direccion = txtDir.Text!.Trim(),
            Telefono = txtTel.Text!.Trim(),
            Email = txtEmail.Text!.Trim(),
            FotoPath = _fotoLocalPath
        };

        await _db.InsertUsuarioAsync(nuevo);
        await DisplayAlert("Registro", "Usuario creado con éxito.", "OK");
        await Navigation.PopAsync();
    }
}
