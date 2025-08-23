using ObligatorioTT.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ObligatorioTT.Services
{
    public static class OpenWeatherService
    {
        private const string ApiKey = "542fd1cfadde57dbc6b00bad2936f5b9";
        private const string Ciudad = "Punta del Este,UY";
        private const string UrlBase = "https://api.openweathermap.org/data/2.5/";

        // ---- helpers de conversión segura ----
        private static decimal? ToDec(double? v) => v is null ? null : (decimal?)Convert.ToDecimal(v.Value);
        private static decimal? ToDec(int? v) => v is null ? null : (decimal?)v.Value;
        private static int? ToInt(int? v) => v;
        private static decimal? MsToKmh(double? v) => v is null ? null : (decimal?)Math.Round(v.Value * 3.6, 1);

        // ----------------- clima actual -----------------
        public static async Task<ClimaModel> ObtenerClimaActualAsync()
        {
            using var client = new HttpClient();
            var url = $"{UrlBase}weather?q={Uri.EscapeDataString(Ciudad)}&appid={ApiKey}&units=metric&lang=es";

            var resp = await client.GetAsync(url);
            if (!resp.IsSuccessStatusCode)
                throw new Exception("No se pudo obtener el clima actual.");

            var data = await resp.Content.ReadFromJsonAsync<ClimaActualResponse>(new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            if (data == null) throw new Exception("Respuesta vacía del servicio.");

            // lluvia: 1h o 3h si existe
            decimal? lluvia = null;
            if (data.rain?.OneH is double r1) lluvia = (decimal)r1;
            else if (data.rain?.ThreeH is double r3) lluvia = (decimal)r3;

            var w = data.weather?.FirstOrDefault();

            return new ClimaModel
            {
                Fecha = DateTime.Now.ToString("dd/MM"),
                Temperatura = ToDec(data.main?.temp),
                Descripcion = w?.description,
                Icono = w?.icon is string ic ? $"https://openweathermap.org/img/wn/{ic}@2x.png" : null,

                TempMax = ToDec(data.main?.temp_max),
                TempMin = ToDec(data.main?.temp_min),
                Humedad = data.main?.humidity,
                Viento = MsToKmh(data.wind?.speed),
                Lluvia = lluvia
            };
        }

        public static async Task<List<ClimaModel>> ObtenerPronosticoAsync()
        {
            using var client = new HttpClient();
            var url = $"{UrlBase}forecast?q={Uri.EscapeDataString(Ciudad)}&appid={ApiKey}&units=metric&lang=es";

            var resp = await client.GetAsync(url);
            if (!resp.IsSuccessStatusCode) return new List<ClimaModel>();

            var data = await resp.Content.ReadFromJsonAsync<ForecastResponse>(new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            if (data?.list == null || data.list.Count == 0) return new List<ClimaModel>();

            // 1) agrupamos por día local
            var grupos = data.list
                .GroupBy(i => DateTimeOffset.FromUnixTimeSeconds(i.dt).ToLocalTime().Date)
                .OrderBy(g => g.Key)
                .Take(5)
                .ToList();

            // Fallback si por alguna razón no agrupó: tomar 5 slots alrededor del mediodía
            if (grupos.Count == 0)
            {
                var medio = data.list.Count / 2;
                var slice = data.list.Skip(Math.Max(0, medio - 2)).Take(5).ToList();
                grupos = slice.GroupBy(i => DateTimeOffset.FromUnixTimeSeconds(i.dt).ToLocalTime().Date).ToList();
            }

            var resultado = new List<ClimaModel>();

            foreach (var g in grupos)
            {
                var items = g.ToList();

                // elegimos el slot representativo (12:00 si existe, si no el del medio)
                var pick = items.FirstOrDefault(i => DateTimeOffset.FromUnixTimeSeconds(i.dt).ToLocalTime().Hour == 12)
                           ?? items[items.Count / 2];

                var min = items.Min(i => i.main?.temp_min ?? i.main?.temp ?? double.NaN);
                var max = items.Max(i => i.main?.temp_max ?? i.main?.temp ?? double.NaN);

                var w = pick.weather?.FirstOrDefault();

                resultado.Add(new ClimaModel
                {
                    Fecha = g.Key.ToString("dd/MM"),
                    Temperatura = ToDec(pick.main?.temp),
                    Descripcion = w?.description,
                    Icono = w?.icon is string ic ? $"https://openweathermap.org/img/wn/{ic}@2x.png" : null,

                    Tmin = double.IsNaN(min) ? null : (decimal?)Convert.ToDecimal(min),
                    Tmax = double.IsNaN(max) ? null : (decimal?)Convert.ToDecimal(max),
                    TieneMaxMin = !(double.IsNaN(min) || double.IsNaN(max))
                });
            }

            return resultado;
        }

        // ----------------- DTOs -----------------
        private class ClimaActualResponse
        {
            public MainInfo? main { get; set; }
            public List<WeatherInfo>? weather { get; set; }
            public WindInfo? wind { get; set; }
            public RainInfo? rain { get; set; }
        }

        private class ForecastResponse
        {
            public List<ForecastItem>? list { get; set; }
        }

        private class ForecastItem
        {
            public long dt { get; set; } // epoch
            public MainInfo? main { get; set; }
            public List<WeatherInfo>? weather { get; set; }
        }

        private class MainInfo
        {
            public double? temp { get; set; }
            public double? temp_min { get; set; }
            public double? temp_max { get; set; }
            public int? humidity { get; set; }
        }

        private class WeatherInfo
        {
            public string? description { get; set; }
            public string? icon { get; set; }
        }

        private class WindInfo
        {
            public double? speed { get; set; } // m/s
        }

        private class RainInfo
        {
            [JsonPropertyName("1h")] public double? OneH { get; set; }
            [JsonPropertyName("3h")] public double? ThreeH { get; set; }
        }
    }
}
