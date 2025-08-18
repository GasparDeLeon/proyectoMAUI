using Microsoft.Maui.Storage;
using ObligatorioTT.Views;

namespace ObligatorioTT;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        var hasSession = Preferences.ContainsKey("LoggedUser");
        Application.Current.UserAppTheme = AppTheme.Dark;



        MainPage = hasSession
            ? new AppShell()
            : new NavigationPage(new LoginPage());
    }
}
