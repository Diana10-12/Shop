using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Polly;
using System.Net; // Для HttpStatusCode

namespace ProcurementApp.Services
{
    public class WeatherService
    {
        private readonly HttpClient _httpClient;
        private const string ApiKey = "26ce76854082020769f3305602c00db5";

        public WeatherService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<WeatherData> GetWeatherDataAsync(double latitude, double longitude)
        {
            try
            {
                var url = $"https://api.openweathermap.org/data/2.5/weather?lat={latitude}&lon={longitude}&units=metric&appid={ApiKey}";

                // Политика повтора (3 попытки с экспоненциальной задержкой)
                var retryPolicy = Policy
                    .Handle<HttpRequestException>()
                    .OrResult<HttpResponseMessage>(r => r.StatusCode == HttpStatusCode.RequestTimeout)
                    .WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)));

                var response = await retryPolicy.ExecuteAsync(() => _httpClient.GetAsync(url));

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Ошибка HTTP: {response.StatusCode}");
                    return null;
                }

                return await response.Content.ReadFromJsonAsync<WeatherData>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка получения погоды: {ex}");
                return null;
            }
        }
    }

    public class WeatherData
    {
        [JsonPropertyName("main")]
        public MainData Main { get; set; }

        [JsonPropertyName("weather")]
        public Weather[] Weather { get; set; }

        [JsonPropertyName("wind")]
        public Wind Wind { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }
    }

    public class Weather
    {
        [JsonPropertyName("main")]
        public string Main { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }
    }

    public class Wind
    {
        [JsonPropertyName("speed")]
        public decimal Speed { get; set; }

        [JsonPropertyName("deg")]
        public int Deg { get; set; }
    }
}
