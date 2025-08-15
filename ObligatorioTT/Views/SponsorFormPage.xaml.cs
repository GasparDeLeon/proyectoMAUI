using System.IO;
using ObligatorioTT.Models;
using ObligatorioTT.Services;

namespace ObligatorioTT.Views
{
    [QueryProperty(nameof(SponsorId), "id")]
    public partial class SponsorFormPage : ContentPage
    {
        public int? SponsorId { get; set; }
        private Sponsor _model = new();

        public SponsorFormPage()
        {
            InitializeComponent(); 
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            await SponsorRepository.Inst.InitAsync();

            if (SponsorId.HasValue)
            {
                var s = await SponsorRepository.Inst.GetAsync(SponsorId.Value);
                if (s != null)
                {
                    _model = s;
                    txtNombre.Text = s.Nombre;
                    txtDireccion.Text = s.Direccion;
                    if (!string.IsNullOrWhiteSpace(s.LogoPath))
                        imgLogo.Source = s.LogoPath;
                }
            }

            ActualizarEstadoGuardar();
        }

        private async void OnElegirLogo(object sender, EventArgs e)
        {
            try
            {
                var file = await MediaPicker.PickPhotoAsync();
                if (file == null) return;

                var dest = Path.Combine(FileSystem.AppDataDirectory,
                                        $"logo_{Guid.NewGuid()}{Path.GetExtension(file.FileName)}");
                using var src = await file.OpenReadAsync();
                using var dst = File.OpenWrite(dest);
                await src.CopyToAsync(dst);

                _model.LogoPath = dest;
                imgLogo.Source = dest;
                ActualizarEstadoGuardar();
            }
            catch
            {
                
            }
        }

        private void OnCampoChanged(object sender, TextChangedEventArgs e)
            => ActualizarEstadoGuardar();

        private void ActualizarEstadoGuardar()
        {
            btnGuardar.IsEnabled =
                !string.IsNullOrWhiteSpace(txtNombre.Text) &&
                !string.IsNullOrWhiteSpace(txtDireccion.Text) &&
                !string.IsNullOrWhiteSpace(_model.LogoPath);
        }

        private async void OnGuardar(object sender, EventArgs e)
        {
            
            if (string.IsNullOrWhiteSpace(txtNombre.Text))
            { await DisplayAlert("Validación", "El nombre es obligatorio.", "OK"); return; }
            if (string.IsNullOrWhiteSpace(txtDireccion.Text))
            { await DisplayAlert("Validación", "La dirección es obligatoria.", "OK"); return; }
            if (string.IsNullOrWhiteSpace(_model.LogoPath))
            { await DisplayAlert("Validación", "Debes seleccionar un logo.", "OK"); return; }

            _model.Nombre = txtNombre.Text.Trim();
            _model.Direccion = txtDireccion.Text.Trim();

            if (_model.Id == 0) await SponsorRepository.Inst.InsertAsync(_model);
            else await SponsorRepository.Inst.UpdateAsync(_model);

            await DisplayAlert("OK", "Patrocinador guardado.", "Cerrar");
            await Shell.Current.GoToAsync("..");
        }
    }
}
