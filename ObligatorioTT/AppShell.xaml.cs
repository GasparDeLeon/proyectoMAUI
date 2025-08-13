using ObligatorioTT.Views;

namespace ObligatorioTT
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

          
            Routing.RegisterRoute("ClimaPage", typeof(ClimaPage));

          

        }
        private async void OnLogoutClicked(object sender, EventArgs e)
        {
            var confirm = await DisplayAlert("Cerrar sesión", "¿Seguro que querés salir?", "Sí", "No");
            if (!confirm) return;

            
            Preferences.Remove("LoggedUser");
            Preferences.Remove("LoggedUserId");
            // Preferences.Remove("LastUserName");

            
            Application.Current.MainPage = new NavigationPage(new LoginPage());
        }
    }
}
