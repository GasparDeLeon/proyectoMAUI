using Microsoft.Maui.Storage;          // Preferences
using ObligatorioTT.Views;

namespace ObligatorioTT;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        Application.Current.UserAppTheme = AppTheme.Dark;

        var hasSession = Preferences.ContainsKey("LoggedUser");
        MainPage = hasSession
            ? new AppShell()
            : new NavigationPage(new LoginPage());
    }

    // Cierra la sesión borrando las claves
    private static void ClearSession()
    {
        Preferences.Remove("LoggedUser");
        Preferences.Remove("LoggedUserId");
    }

    // Se llama cuando la app pasa a background / se detiene
    protected override void OnSleep()
    {
        base.OnSleep();
        ClearSession();
    }
}
