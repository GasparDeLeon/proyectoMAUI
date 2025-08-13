using Microsoft.Maui.Storage;
using ObligatorioTT.Views;

namespace ObligatorioTT;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        var hasSession = Preferences.ContainsKey("LoggedUser");

        
        MainPage = hasSession
            ? new AppShell()
            : new NavigationPage(new LoginPage());
    }
}
