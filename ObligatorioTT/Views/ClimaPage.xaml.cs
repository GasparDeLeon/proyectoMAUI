using Microsoft.Maui.Controls;
using ObligatorioTT.Services;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace ObligatorioTT.Views
{
    public partial class ClimaPage : ContentPage
    {
        private bool _isBusy;

        public ClimaPage()
        {
            InitializeComponent();
            _ = CargarClimaAsync();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
        }

        private void SetBusy(bool on)
        {
            _isBusy = on;
            busyOverlay.IsVisible = on;
        }


        private static object? GetPropObj(object src, string name)
        {
            if (src == null) return null;

            if (src is IDictionary dict)
            {
                foreach (var key in dict.Keys)
                {
                    if (key?.ToString()?.Equals(name, StringComparison.OrdinalIgnoreCase) == true)
                        return dict[key];
                }
            }

            var t = src.GetType();

            if (t.IsGenericType && t.Name.StartsWith("KeyValuePair", StringComparison.Ordinal))
            {
                var k = t.GetProperty("Key")?.GetValue(src)?.ToString();
                if (k?.Equals(name, StringComparison.OrdinalIgnoreCase) == true)
                    return t.GetProperty("Value")?.GetValue(src);
            }

            var p = t.GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            return p?.GetValue(src);
        }

        private static object? GetByPath(object src, string path)
        {
            var parts = path.Split('.', StringSplitOptions.RemoveEmptyEntries);
            object? cur = src;
            foreach (var part in parts)
            {
                if (cur == null) return null;

                object? next = GetPropObj(cur, part);
                if (next == null && cur is IDictionary dict)
                {
                    if (dict.Contains(part)) next = dict[part];
                }
                cur = next;
            }
            return cur;
        }

        private static bool TryToDouble(object? val, out double d)
        {
            d = double.NaN;
            if (val == null) return false;

            if (val is double dd) { d = dd; return true; }
            if (val is float ff) { d = ff; return true; }
            if (val is decimal mm) { d = (double)mm; return true; }
            if (val is int ii) { d = ii; return true; }
            if (val is long ll) { d = ll; return true; }

            var s = val.ToString()?.Trim();
            if (string.IsNullOrWhiteSpace(s)) return false;

            s = s.Replace("km/h", "", StringComparison.OrdinalIgnoreCase)
                 .Replace("m/s", "", StringComparison.OrdinalIgnoreCase)
                 .Replace("%", "", StringComparison.OrdinalIgnoreCase)
                 .Replace("mm", "", StringComparison.OrdinalIgnoreCase)
                 .Trim();

            return double.TryParse(s, System.Globalization.NumberStyles.Float,
                                   System.Globalization.CultureInfo.InvariantCulture, out d);
        }

        private static double FirstDouble(object src, params string[] candidates)
        {
            foreach (var c in candidates)
            {
                var raw = c.Contains('.') ? GetByPath(src, c) : GetPropObj(src, c);
                if (TryToDouble(raw, out var d)) return d;
            }
            return double.NaN;
        }

        private static string FirstString(object src, params string[] candidates)
        {
            foreach (var c in candidates)
            {
                var raw = c.Contains('.') ? GetByPath(src, c) : GetPropObj(src, c);
                var s = raw?.ToString();
                if (!string.IsNullOrWhiteSpace(s)) return s!;
            }
            return "--";
        }

        private static string ToIconUrl(string? iconOrUrl)
        {
            if (string.IsNullOrWhiteSpace(iconOrUrl)) return "";

            if (Uri.TryCreate(iconOrUrl, UriKind.Absolute, out var u))
            {
                if (u.Scheme.Equals("http", StringComparison.OrdinalIgnoreCase))
                    return "https" + iconOrUrl.Substring(4);
                return iconOrUrl;
            }

            return $"https://openweathermap.org/img/wn/{iconOrUrl}@2x.png";
        }


        private async Task CargarClimaAsync()
        {
            if (_isBusy) return;

            try
            {
                SetBusy(true);

                var climaActual = await OpenWeatherService.ObtenerClimaActualAsync();

                lblFechaActual.Text = FirstString(climaActual, "Fecha", "FechaTexto", "DateText", "DtTxt", "Date");
                lblTempActual.Text = $"{FirstDouble(climaActual, "Temperatura", "TempC", "Temp", "Main.Temp"):0} °C";
                lblDescripcionActual.Text = FirstString(climaActual, "Descripcion", "Estado", "Resumen", "Weather.Description");

                var iconUrl = ToIconUrl(FirstString(climaActual, "Icono", "Icon", "IconUrl", "Weather.Icon"));
                imgIconoActual.Source = new UriImageSource
                {
                    Uri = string.IsNullOrWhiteSpace(iconUrl) ? null : new Uri(iconUrl),
                    CachingEnabled = true,
                    CacheValidity = TimeSpan.FromHours(6)
                };

                var tMax = FirstDouble(climaActual, "TempMax", "TemperaturaMaxima", "Tmax", "Main.TempMax");
                var tMin = FirstDouble(climaActual, "TempMin", "TemperaturaMinima", "Tmin", "Main.TempMin");
                lblMax.Text = double.IsNaN(tMax) ? "-- °C" : $"{tMax:0} °C";
                lblMin.Text = double.IsNaN(tMin) ? "-- °C" : $"{tMin:0} °C";

                var windKmh = FirstDouble(climaActual, "Viento", "VientoKmh", "WindKmh", "WindSpeedKmh");
                if (double.IsNaN(windKmh))
                {
                    var windMs = FirstDouble(climaActual, "VientoMs", "WindMs", "WindSpeed", "Wind.Speed");
                    if (!double.IsNaN(windMs)) windKmh = windMs * 3.6;
                }

                var hum = FirstDouble(climaActual, "Humedad", "Humidity", "Main.Humidity");

                var rain = FirstDouble(climaActual, "Lluvia", "PrecipitacionMm", "LluviaMm", "RainMm", "Rain",
                                                     "Rain.1h", "Rain.3h", "Precipitation", "Snow.1h", "Snow.3h");

                lblViento.Text = double.IsNaN(windKmh) ? "— km/h" : $"{windKmh:0} km/h";
                lblHumedad.Text = double.IsNaN(hum) ? "— %" : $"{hum:0}%";
                lblLluvia.Text = double.IsNaN(rain) ? "— mm" : $"{rain:0.0} mm";


                var pronostico = await OpenWeatherService.ObtenerPronosticoAsync();

                if (pronostico != null)
                {
                    foreach (var item in pronostico)
                    {
                        var t = item.GetType();
                        var pIcon = t.GetProperty("Icono", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                        if (pIcon?.CanWrite == true)
                        {
                            var val = pIcon.GetValue(item)?.ToString();
                            pIcon.SetValue(item, ToIconUrl(val));
                        }

                        var maxD = FirstDouble(item, "TempMax", "TemperaturaMaxima", "Tmax", "Main.TempMax");
                        var minD = FirstDouble(item, "TempMin", "TemperaturaMinima", "Tmin", "Main.TempMin");

                        decimal? maxDec = double.IsNaN(maxD) ? (decimal?)null : (decimal?)Convert.ToDecimal(Math.Round(maxD, 0));
                        decimal? minDec = double.IsNaN(minD) ? (decimal?)null : (decimal?)Convert.ToDecimal(Math.Round(minD, 0));

                        var pTmax = t.GetProperty("Tmax", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                        if (pTmax?.CanWrite == true) pTmax.SetValue(item, maxDec);

                        var pTmin = t.GetProperty("Tmin", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                        if (pTmin?.CanWrite == true) pTmin.SetValue(item, minDec);

                        var pTiene = t.GetProperty("TieneMaxMin", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                        if (pTiene?.CanWrite == true) pTiene.SetValue(item, maxDec.HasValue && minDec.HasValue);
                    }
                }

                climaCollection.ItemsSource = pronostico;

                lblActualizado.Text = $"Actualizado: {DateTime.Now:dd/MM/yyyy HH:mm}";
            }
            catch (Exception ex)
            {
                await DisplayAlert("Clima", "No se pudo cargar el clima.\n" + ex.Message, "OK");
            }
            finally
            {
                SetBusy(false);
            }
        }

        private async void RefreshView_Refreshing(object sender, EventArgs e)
        {
            await CargarClimaAsync();
            if (sender is RefreshView rv) rv.IsRefreshing = false;
        }

        private async void Reintentar_Clicked(object sender, EventArgs e)
        {
            await CargarClimaAsync();
        }

        private async void Volver_Clicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//MainPage");
        }
    }
}
