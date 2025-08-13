using ObligatorioTT.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace ObligatorioTT.Services
{
        public static class OpenWeatherService
        {
            private const string ApiKey = "542fd1cfadde57dbc6b00bad2936f5b9"; 
            private const string Ciudad = "Punta del Este,UY";
            private const string UrlBase = "https://api.openweathermap.org/data/2.5/";

            public static async Task<List<ClimaModel>> ObtenerPronosticoAsync()
            {
                using var client = new HttpClient();
                var url = $"{UrlBase}forecast?q={Ciudad}&appid={ApiKey}&units=metric&lang=es";

                var response = await client.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                    return new List<ClimaModel>();

                var data = await response.Content.ReadFromJsonAsync<ForecastResponse>();

                return data.list
                    .Where(x => x.dt_txt.Contains("12:00:00"))
                    .Select(x => new ClimaModel
                    {
                        Fecha = DateTime.Parse(x.dt_txt).ToString("dd/MM"),
                        Temperatura = x.main.temp,
                        Descripcion = x.weather[0].description,
                        Icono = $"https://openweathermap.org/img/wn/{x.weather[0].icon}@2x.png"
                    })
                    .ToList();
            }
        public static async Task<ClimaModel> ObtenerClimaActualAsync()
        {
            using var client = new HttpClient();
            var url = $"{UrlBase}weather?q={Ciudad}&appid={ApiKey}&units=metric&lang=es";

            var response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                throw new Exception("No se pudo obtener el clima actual.");

            var data = await response.Content.ReadFromJsonAsync<ClimaActualResponse>();

            return new ClimaModel
            {
                Fecha = DateTime.Now.ToString("dd/MM"),
                Temperatura = data.main.temp,
                Descripcion = data.weather[0].description,
                Icono = $"https://openweathermap.org/img/wn/{data.weather[0].icon}@2x.png"
            };
        }

        private class ClimaActualResponse
        {
            public MainInfo main { get; set; }
            public List<WeatherInfo> weather { get; set; }
        }



        private class ForecastResponse
            {
                public List<ForecastItem> list { get; set; }
            }

            private class ForecastItem
            {
                public MainInfo main { get; set; }
                public List<WeatherInfo> weather { get; set; }
                public string dt_txt { get; set; }
            }

            private class MainInfo
            {
                public decimal temp { get; set; }
            }

            private class WeatherInfo
            {
                public string description { get; set; }
                public string icon { get; set; }
            }
        }
    
}
