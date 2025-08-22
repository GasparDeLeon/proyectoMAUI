#if ANDROID
using System;
using System.Linq;
using Microsoft.Maui.Controls;
using ObligatorioTT.Models;
using ObligatorioTT.Services;

using ControlsMaps = Microsoft.Maui.Controls.Maps;
using MauiMaps = Microsoft.Maui.Maps;

using Microsoft.Maui.Devices.Sensors;
using ControlsMapClickedEventArgs = Microsoft.Maui.Controls.Maps.MapClickedEventArgs;

namespace ObligatorioTT.Views
{
    [QueryProperty(nameof(SponsorIdQuery), "id")]
    public class PinPickerPage : ContentPage
    {
        public int SponsorId { get; private set; }

        public string? SponsorIdQuery
        {
            get => SponsorId.ToString();
            set
            {
                if (int.TryParse(value, out var id))
                    SponsorId = id;
            }
        }

        private ControlsMaps.Map _map;
        private ControlsMaps.Pin? _pinActual;
        private Sponsor? _sponsor;

        private Button _btnGuardar;

        public PinPickerPage()
        {
            Title = "Ubicación del patrocinador";

            _map = new ControlsMaps.Map
            {
                MapType = MauiMaps.MapType.Street,
                IsShowingUser = false,
                VerticalOptions = LayoutOptions.FillAndExpand
            };
            _map.MapClicked += OnMapClicked;

            var lbl = new Label
            {
                Text = "Tocá el mapa para fijar el pin y luego guardá.",
                Margin = new Thickness(12, 8),
                FontSize = 14
            };

            _btnGuardar = new Button
            {
                Text = "Guardar ubicación",
                Margin = new Thickness(12, 8),
                IsEnabled = false
            };
            _btnGuardar.Clicked += OnGuardarUbicacion;

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            grid.Children.Add(lbl); Grid.SetRow(lbl, 0);
            grid.Children.Add(_map); Grid.SetRow(_map, 1);
            grid.Children.Add(_btnGuardar); Grid.SetRow(_btnGuardar, 2);

            Content = grid;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            await SponsorRepository.Inst.InitAsync();
            _sponsor = await SponsorRepository.Inst.GetAsync(SponsorId);

            if (_sponsor?.Latitud is double lat && _sponsor.Longitud is double lng)
            {
                var center = new Location(lat, lng);
                _map.MoveToRegion(MauiMaps.MapSpan.FromCenterAndRadius(center, MauiMaps.Distance.FromKilometers(1)));
                ColocarOActualizarPin(center);
            }
            else if (!string.IsNullOrWhiteSpace(_sponsor?.Direccion))
            {
                try
                {
                    var results = await Geocoding.GetLocationsAsync(_sponsor.Direccion);
                    var loc = results?.FirstOrDefault();
                    if (loc != null)
                    {
                        _map.MoveToRegion(MauiMaps.MapSpan.FromCenterAndRadius(loc, MauiMaps.Distance.FromKilometers(1.5)));
                    }
                    else
                    {
                        var fallback = new Location(-34.9, -54.95);
                        _map.MoveToRegion(MauiMaps.MapSpan.FromCenterAndRadius(fallback, MauiMaps.Distance.FromKilometers(5)));
                    }
                }
                catch
                {
                    var fallback = new Location(-34.9, -54.95);
                    _map.MoveToRegion(MauiMaps.MapSpan.FromCenterAndRadius(fallback, MauiMaps.Distance.FromKilometers(5)));
                }
            }
        }

        private void OnMapClicked(object? sender, ControlsMapClickedEventArgs e)
        {
            ColocarOActualizarPin(e.Location);
        }

        // Forzar refresco del marcador: limpiar y re-crear el pin
        private void ColocarOActualizarPin(Location loc)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                _map.Pins.Clear();
                _pinActual = new ControlsMaps.Pin
                {
                    Label = _sponsor?.Nombre ?? "Patrocinador",
                    Address = _sponsor?.Direccion ?? "",
                    Type = ControlsMaps.PinType.Place,
                    Location = loc
                };
                _map.Pins.Add(_pinActual);

                _map.MoveToRegion(MauiMaps.MapSpan.FromCenterAndRadius(
                    loc, MauiMaps.Distance.FromKilometers(1)));

                _btnGuardar.IsEnabled = true;
            });
        }

   private async void OnGuardarUbicacion(object? sender, EventArgs e)
{
    if (_sponsor is null || _pinActual is null) return;

    try
    {
        _btnGuardar.IsEnabled = false;

        _sponsor.Latitud  = _pinActual.Location.Latitude;
        _sponsor.Longitud = _pinActual.Location.Longitude;

        await SponsorRepository.Inst.UpdateAsync(_sponsor);

        // Confirmación
        await DisplayAlert("OK", "Ubicación guardada.", "Cerrar");

        // 1) Ir al root de Patrocinadores para resetear su stack
        await Shell.Current.GoToAsync("//PatrocinadoresPage");

        // (opcional, por si hubiera subpáginas colgadas): limpiar stack
        await Shell.Current.Navigation.PopToRootAsync(false);

        // 2) Ir a Inicio
        await Shell.Current.GoToAsync("//MainPage");
    }
    catch (Exception ex)
    {
        await DisplayAlert("Error", $"No se pudo guardar la ubicación.\n{ex.Message}", "Cerrar");
    }
    finally
    {
        _btnGuardar.IsEnabled = true;
    }
}


    }
}
#endif
