using System;
using System.Globalization;
using System.Threading.Tasks;
using RestSharp;
using Newtonsoft.Json.Linq;

namespace ProcurementApp.Services
{
    public class YandexMapService
    {
        private const string ApiKey = "8721f8f4-879d-4550-9dad-abcf83628e3d";

        public string GenerateMapHtml(double latitude, double longitude)
        {
            return $@"
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset='utf-8' />
            <meta name='viewport' content='width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no' />
            <title>Карта</title>
            <script src='https://api-maps.yandex.ru/2.1/?apikey= {ApiKey}&lang=ru_RU' type='text/javascript'></script>
            <style>
                html, body, #map {{
                    width: 100%;
                    height: 100%;
                    margin: 0;
                    padding: 0;
                    overflow: hidden;
                }}
            </style>
        </head>
        <body>
            <div id='map'></div>
            <script type='text/javascript'>
                ymaps.ready(init);
                function init() {{
                    var myMap = new ymaps.Map('map', {{
                        center: [{latitude.ToString(CultureInfo.InvariantCulture)}, {longitude.ToString(CultureInfo.InvariantCulture)}],
                        zoom: 15,
                        controls: ['zoomControl']
                    }});
                    
                    var myPlacemark = new ymaps.Placemark(myMap.getCenter(), {{
                        hintContent: 'Местоположение',
                        balloonContent: 'Выбранный адрес'
                    }});
                    
                    myMap.geoObjects.add(myPlacemark);
                }}
            </script>
        </body>
        </html>";
        }

        public async Task<(double Latitude, double Longitude)> GetCoordinatesAsync(string address)
        {
            try
            {
                Console.WriteLine($"Геокодирование адреса: {address}");

                if (string.IsNullOrWhiteSpace(address))
                {
                    throw new ArgumentException("Адрес не может быть пустым");
                }

                var client = new RestClient("https://geocode-maps.yandex.ru/1.x/ ");
                var request = new RestRequest();
                request.AddParameter("apikey", ApiKey);
                request.AddParameter("geocode", address);
                request.AddParameter("format", "json");

                var response = await client.ExecuteAsync(request);

                if (!response.IsSuccessful)
                {
                    Console.WriteLine($"Ошибка HTTP-запроса: {response.StatusCode}, Тело: {response.Content}");
                    throw new Exception($"Геокодирование не выполнено: {response.StatusCode}");
                }

                JObject json;
                try
                {
                    json = JObject.Parse(response.Content);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка парсинга JSON: {ex.Message}");
                    throw new FormatException("Не удалось разобрать ответ от сервера");
                }

                var featureMember = json["response"]?["GeoObjectCollection"]?["featureMember"]?[0];

                if (featureMember == null)
                {
                    throw new Exception("Нет результатов для указанного адреса");
                }

                var pos = featureMember["GeoObject"]?["Point"]?["pos"]?.ToString();

                if (string.IsNullOrEmpty(pos))
                {
                    throw new Exception("Некорректный ответ геокодирования");
                }

                // Исправляем порядок координат: Yandex возвращает долготу шириной, а нам нужно (широта, долгота)
                var coords = pos.Split(' ');
                double lat = double.Parse(coords[1], CultureInfo.InvariantCulture);
                double lon = double.Parse(coords[0], CultureInfo.InvariantCulture);

                Console.WriteLine($"Координаты получены: {lat}, {lon}");

                return (Latitude: lat, Longitude: lon);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка геокодирования: {ex}");
                throw new ApplicationException("Не удалось определить координаты. Проверьте формат адреса.");
            }
        }

        public async Task<string> GetAddressAsync(double latitude, double longitude)
        {
            var client = new RestClient("https://geocode-maps.yandex.ru/1.x/ ");
            var request = new RestRequest();
            request.AddParameter("apikey", ApiKey);
            request.AddParameter("geocode", $"{longitude.ToString(CultureInfo.InvariantCulture)},{latitude.ToString(CultureInfo.InvariantCulture)}");
            request.AddParameter("format", "json");

            var response = await client.ExecuteAsync(request);

            if (!response.IsSuccessful)
            {
                Console.WriteLine($"Ошибка получения адреса: {response.StatusCode}");
                throw new Exception("Не удалось получить адрес по координатам");
            }

            JObject json;
            try
            {
                json = JObject.Parse(response.Content);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка парсинга JSON при получении адреса: {ex.Message}");
                throw new FormatException("Не удалось разобрать ответ от сервера");
            }

            var address = json["response"]?["GeoObjectCollection"]?["featureMember"]?[0]?["GeoObject"]?["metaDataProperty"]?["GeocoderMetaData"]?["text"]?.ToString();

            if (string.IsNullOrEmpty(address))
            {
                throw new Exception("Не удалось извлечь адрес из ответа сервиса");
            }

            return address;
        }
    }
}
