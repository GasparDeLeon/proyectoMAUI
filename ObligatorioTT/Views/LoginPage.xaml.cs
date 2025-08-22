using System.Linq;
using Microsoft.Maui.Storage;
using Microsoft.Maui.ApplicationModel; // DeviceInfo
using ObligatorioTT.Data;
using ObligatorioTT.Utils;
using ObligatorioTT.Services;
using ObligatorioTT.Helpers;

namespace ObligatorioTT.Views;

public partial class LoginPage : ContentPage
{
    private readonly IBiometricAuthService _bio;
    private readonly DatabaseService _db;
    private bool _isBusy;

    public LoginPage() : this(
        ServiceHelper.GetService<IBiometricAuthService>(),
        ServiceHelper.GetService<DatabaseService>())
    { }

    public LoginPage(IBiometricAuthService bio, DatabaseService db)
    {
        InitializeComponent();
        _bio = bio;
        _db = db;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Si ya hay sesi�n, entrar directo
        if (Preferences.ContainsKey("LoggedUser"))
        {
            Application.Current.MainPage = new AppShell();
            return;
        }

        // Prefill usuario si existe
        var lastUser = Preferences.Get("LastUserName", "");
        if (!string.IsNullOrWhiteSpace(lastUser))
        {
            txtUser.Text = lastUser;
        }

        // Mostrar/ocultar bot�n biom�trico seg�n disponibilidad
        try
        {
            var bioOk = await _bio.IsAvailableAsync();
            btnBio.IsVisible = bioOk;
            if (bioOk)
            {
                string bioText = "Ingresar con biometr�a";
                if (DeviceInfo.Platform == DevicePlatform.iOS) bioText = "Ingresar con Face/Touch ID";
                else if (DeviceInfo.Platform == DevicePlatform.Android) bioText = "Ingresar con huella";
                btnBio.Text = bioText;
            }
        }
        catch
        {
            btnBio.IsVisible = false;
        }
    }

    // ------- Helpers de UI -------
    private void SetBusy(bool on)
    {
        _isBusy = on;
        busyOverlay.IsVisible = on;
        btnIngresar.IsEnabled = !on;
        btnBio.IsEnabled = !on;
    }

    private void ClearError(string? msg = null)
    {
        lblError.Text = msg ?? "";
        lblError.IsVisible = !string.IsNullOrWhiteSpace(lblError.Text);
    }

    // Mover foco con Enter
    private void User_Completed(object? sender, EventArgs e) => txtPass?.Focus();

    private async void Pass_Completed(object? sender, EventArgs e)
    {
        await LoginAsync(); // Enter en contrase�a hace login
    }

    private void TogglePassword_Clicked(object? sender, EventArgs e)
    {
        if (txtPass == null) return;
        txtPass.IsPassword = !txtPass.IsPassword;
        btnTogglePass.Text = txtPass.IsPassword ? "Mostrar" : "Ocultar";
    }

    // ------- Acciones -------
    private async void BtnIngresar_Clicked(object sender, EventArgs e) => await LoginAsync();

    private async Task LoginAsync()
    {
        if (_isBusy) return;
        try
        {
            ClearError();
            SetBusy(true);

            var user = txtUser.Text?.Trim();
            var pass = txtPass.Text ?? "";

            if (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(pass))
            {
                ClearError("Usuario y contrase�a son obligatorios.");
                return;
            }

            var u = await _db.GetUsuarioByUserAsync(user);
            if (u == null || u.Password != SecurityHelper.Sha256(pass))
            {
                ClearError("Credenciales inv�lidas.");
                return;
            }

            Preferences.Set("LoggedUser", u.UserName);
            Preferences.Set("LoggedUserId", u.Id);

            // Si NO quer�s guardar m�s el �ltimo usuario, coment� la l�nea siguiente:
            Preferences.Set("LastUserName", u.UserName);

            Application.Current.MainPage = new AppShell();
        }
        catch
        {
            ClearError("Ocurri� un error al iniciar sesi�n.");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void BtnBio_Clicked(object sender, EventArgs e)
    {
        if (_isBusy) return;

        try
        {
            ClearError();
            SetBusy(true);

            if (!await _bio.IsAvailableAsync())
            {
                ClearError("Biometr�a no disponible en este dispositivo.");
                return;
            }

            var ok = await _bio.AuthenticateAsync("Acceder con biometr�a");
            if (!ok) return;

            // Tomar �ltimo usuario recordado o el primero
            var userName = Preferences.Get("LastUserName", "");
            var usr = !string.IsNullOrEmpty(userName)
                ? await _db.GetUsuarioByUserAsync(userName)
                : (await _db.GetUsuariosAsync()).FirstOrDefault();

            if (usr == null)
            {
                ClearError("No hay usuarios registrados a�n.");
                return;
            }

            Preferences.Set("LoggedUser", usr.UserName);
            Preferences.Set("LoggedUserId", usr.Id);
            Preferences.Set("LastUserName", usr.UserName);

            Application.Current.MainPage = new AppShell();
        }
        catch
        {
            ClearError("No se pudo autenticar con biometr�a.");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void BtnIrRegistro_Clicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new RegistroPage(_db));
    }
}
