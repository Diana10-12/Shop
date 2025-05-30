using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ProcurementApp.Services
{
    public class DeliveryCalculator
    {
        private readonly WeatherService _weatherService;

        public DeliveryCalculator(WeatherService weatherService)
        {
            _weatherService = weatherService;
        }

        public async Task<(decimal cost, int days, string impact)> CalculateDeliveryAsync(
            double latitude,
            double longitude,
            decimal basePrice,
            int baseDays)
        {
            try
            {
                // Логирование координат для отладки
                Console.WriteLine($"Расчет доставки для координат: {latitude}, {longitude}");

                var weather = await _weatherService.GetWeatherDataAsync(latitude, longitude);

                if (weather == null)
                {
                    return (basePrice * 1.5m, baseDays * 2,
                        "Не удалось получить погодные данные");
                }

                // Логирование полученных данных
                Console.WriteLine($"Получены погодные данные: Temp={weather.Main.Temp}°C, Conditions={string.Join(",", weather.Weather.Select(w => w.Main))}");

                decimal multiplier = 1.0m;
                int daysMultiplier = 1;
                var impact = new List<string>();

                decimal tempCelsius = weather.Main.Temp;

                // Температурные коэффициенты
                if (tempCelsius < -20)
                {
                    multiplier += 0.4m;
                    daysMultiplier += 2;
                    impact.Add("экстремальный холод");
                }
                else if (tempCelsius < 0)
                {
                    multiplier += 0.2m;
                    daysMultiplier += 1;
                    impact.Add("мороз");
                }
                else if (tempCelsius > 35)
                {
                    multiplier += 0.3m;
                    daysMultiplier += 2;
                    impact.Add("экстремальная жара");
                }

                // Осадки
                if (weather.Weather.Any(w => w.Main == "Rain"))
                {
                    multiplier += 0.25m;
                    daysMultiplier += 1;
                    impact.Add("сильный дождь");
                }
                else if (weather.Weather.Any(w => w.Main == "Snow"))
                {
                    multiplier += 0.35m;
                    daysMultiplier += 2;
                    impact.Add("снегопад");
                }

                var finalPrice = basePrice * multiplier;
                var finalDays = baseDays * daysMultiplier;

                var impactDescription = impact.Count > 0
                    ? $"Погодные условия: {string.Join(", ", impact)}"
                    : "Идеальные погодные условия для доставки";

                return (finalPrice, finalDays, impactDescription);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка расчета доставки: {ex}");
                return (basePrice * 1.5m, baseDays * 2,
                    $"Ошибка расчета: {ex.Message}");
            }
        }
    }
}
