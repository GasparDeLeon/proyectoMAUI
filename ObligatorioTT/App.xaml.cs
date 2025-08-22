using Microsoft.Maui.Storage;          // Preferences
using ObligatorioTT.Views;

namespace ObligatorioTT;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        Application.Current.UserAppTheme = AppTheme.Dark;

        // 🔴 Limpia siempre la sesión al iniciar la app
        Preferences.Remove("LoggedUser");
        Preferences.Remove("LoggedUserId");

        // Siempre arranca en Login
        MainPage = new NavigationPage(new LoginPage());
    }

}
