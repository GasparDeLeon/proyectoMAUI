
using ObligatorioTT.Models;
using ObligatorioTT.Services;

namespace ObligatorioTT.Views
{
    public partial class SponsorsPage : ContentPage
    {
        public SponsorsPage()
        {
            InitializeComponent(); 
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await SponsorRepository.Inst.InitAsync();
            await CargarAsync();
        }

        private async Task CargarAsync(string filtro = null)
        {
            cv.ItemsSource = await SponsorRepository.Inst.GetAllAsync(filtro);
        }

        private async void OnBuscar(object sender, EventArgs e)
        {
            await CargarAsync(search.Text);
        }

        private async void OnNuevoClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync(nameof(SponsorFormPage));
        }

        private async void OnEditar(object sender, EventArgs e)
        {
            if ((sender as Button)?.CommandParameter is Sponsor s)
                await Shell.Current.GoToAsync($"{nameof(SponsorFormPage)}?id={s.Id}");
        }

        private async void OnEliminar(object sender, EventArgs e)
        {
            if ((sender as Button)?.CommandParameter is Sponsor s)
            {
                var ok = await DisplayAlert("Eliminar", $"¿Eliminar '{s.Nombre}'?", "Sí", "No");
                if (!ok) return;
                await SponsorRepository.Inst.DeleteAsync(s);
                await CargarAsync(search.Text);
            }
        }
    }
}
