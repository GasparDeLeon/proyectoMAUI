using System.IO;
using System.Linq; // FirstOrDefault
using ObligatorioTT.Models;
using ObligatorioTT.Services;

#if ANDROID
using Microsoft.Maui.Devices.Sensors; // Geocoding en Android
#endif

namespace ObligatorioTT.Views
{
    [QueryProperty(nameof(SponsorIdQuery), "id")]
    public partial class SponsorFormPage : ContentPage
    {
        public int? SponsorId { get; set; }

        // Recibe el par�metro de ruta "id" como string y lo convierte a int?
        public string? SponsorIdQuery
        {
            get => SponsorId?.ToString();
            set => SponsorId = int.TryParse(value, out var id) ? id : null;
        }

        private Sponsor _model = new();

        // Para detectar si cambi� la direcci�n en edici�n
        private string? _direccionOriginal;

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
                    _direccionOriginal = s.Direccion;

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
                // Silencioso por ahora; podr�as mostrar un DisplayAlert si quer�s
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
            { await DisplayAlert("Validaci�n", "El nombre es obligatorio.", "OK"); return; }

            if (string.IsNullOrWhiteSpace(txtDireccion.Text))
            { await DisplayAlert("Validaci�n", "La direcci�n es obligatoria.", "OK"); return; }

            if (string.IsNullOrWhiteSpace(_model.LogoPath))
            { await DisplayAlert("Validaci�n", "Debes seleccionar un logo.", "OK"); return; }

            // Actualizar modelo desde el formulario
            _model.Nombre = txtNombre.Text.Trim();
            _model.Direccion = txtDireccion.Text.Trim();

#if ANDROID
            // Geocodificar SOLO en Android:
            // - Si es alta (Id == 0), siempre geocodificamos.
            // - Si es edici�n, geocodificamos solo si cambi� la direcci�n.
            bool debeGeocodificar = _model.Id == 0 ||
                                    !string.Equals(_direccionOriginal, _model.Direccion, StringComparison.OrdinalIgnoreCase);

            if (debeGeocodificar && !string.IsNullOrWhiteSpace(_model.Direccion))
            {
                try
                {
                    var results = await Geocoding.GetLocationsAsync(_model.Direccion);
                    var loc = results?.FirstOrDefault();
                    if (loc != null)
                    {
                        _model.Latitud = loc.Latitude;
                        _model.Longitud = loc.Longitude;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[Geocoding] {ex.Message}");
                    // Si falla, dejamos Lat/Long en null y seguimos guardando igual
                }
            }
#endif

            if (_model.Id == 0)
            {
                await SponsorRepository.Inst.InsertAsync(_model);

                // ?? ANDROID: ir a la p�gina para fijar el pin del nuevo sponsor
#if ANDROID
                await Shell.Current.GoToAsync($"PinPickerPage?id={_model.Id}");
                return; // evitamos mostrar el alert y volver dos veces
#endif
            }
            else
            {
                await SponsorRepository.Inst.UpdateAsync(_model);
            }

            await DisplayAlert("OK", "Patrocinador guardado.", "Cerrar");
            await Shell.Current.GoToAsync("..");
        }
    }
}
