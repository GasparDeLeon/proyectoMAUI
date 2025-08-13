using System.Linq;
using Microsoft.Maui.Storage;
using ObligatorioTT.Data;
using ObligatorioTT.Utils;
using ObligatorioTT.Services;
using ObligatorioTT.Helpers;

namespace ObligatorioTT.Views;

public partial class LoginPage : ContentPage
{
    private readonly IBiometricAuthService _bio;
    private readonly DatabaseService _db;


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

      
        if (Preferences.ContainsKey("LoggedUser"))
        {
            Application.Current.MainPage = new AppShell();
            return;
        }
    }

    private async void BtnIngresar_Clicked(object sender, EventArgs e)
    {
        var user = txtUser.Text?.Trim();
        var pass = txtPass.Text ?? "";

        if (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(pass))
        {
            await DisplayAlert("Login", "Usuario y contrase�a son obligatorios.", "OK");
            return;
        }

        var u = await _db.GetUsuarioByUserAsync(user);
        if (u == null || u.Password != SecurityHelper.Sha256(pass))
        {
            await DisplayAlert("Login", "Credenciales inv�lidas.", "OK");
            return;
        }

        Preferences.Set("LoggedUser", u.UserName);
        Preferences.Set("LoggedUserId", u.Id);
        Preferences.Set("LastUserName", u.UserName);

        
        Application.Current.MainPage = new AppShell();
    }

    private async void BtnBio_Clicked(object sender, EventArgs e)
    {
        if (!await _bio.IsAvailableAsync())
        {
            await DisplayAlert("Biometr�a", "No disponible en este dispositivo.", "OK");
            return;
        }

        var ok = await _bio.AuthenticateAsync("Acceder con biometr�a");
        if (!ok) return;

        
        var userName = Preferences.Get("LastUserName", "");
        var usr = !string.IsNullOrEmpty(userName)
            ? await _db.GetUsuarioByUserAsync(userName)
            : (await _db.GetUsuariosAsync()).FirstOrDefault();

        if (usr == null)
        {
            await DisplayAlert("Biometr�a", "No hay usuarios registrados a�n.", "OK");
            return;
        }

        Preferences.Set("LoggedUser", usr.UserName);
        Preferences.Set("LoggedUserId", usr.Id);
        Preferences.Set("LastUserName", usr.UserName);

       
        Application.Current.MainPage = new AppShell();
    }

    private async void BtnIrRegistro_Clicked(object sender, EventArgs e)
    {
       
        await Navigation.PushAsync(new RegistroPage(_db));
    }
}
