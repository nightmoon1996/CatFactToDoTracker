using Microsoft.EntityFrameworkCore;
using TodoList.Data;
using TodoList.Models;
using System.Globalization; // Add for date formatting

namespace TodoList.Services
{
    public class TodoService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;

        public TodoService(ApplicationDbContext context, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<TodoItem?> CreateTodoAsync(TodoCreateModel model, int userId)
        {
            string catFact = await GetCatFactAsync();

            var todoItem = new TodoItem
            {
                Message = model.Message,
                Date = model.Date,
                CatFact = catFact,
                UserId = userId
            };

            _context.TodoItems.Add(todoItem);
            await _context.SaveChangesAsync();

            return todoItem;
        }

        public async Task<IEnumerable<TodoViewModel>> GetTodosByUserIdAsync(int userId)
        {
            var todoItems = await _context.TodoItems
                .Where(t => t.UserId == userId)
                .ToListAsync(); // Fetch items first

            var todoViewModels = new List<TodoViewModel>();
            foreach (var t in todoItems)
            {
                // Fetch weather for each item's date
                string weatherDesc = await GetWeatherDescriptionAsync(t.Date);

                todoViewModels.Add(new TodoViewModel
                {
                    Id = t.Id,
                    Message = t.Message,
                    Date = t.Date,
                    CatFact = t.CatFact,
                    WeatherDescription = weatherDesc // Populate the weather description
                });
            }
            return todoViewModels;
        }

        private async Task<string> GetCatFactAsync()
        {
            try
            {
                var client = _httpClientFactory.CreateClient("CatFactClient");
                var response = await client.GetAsync("fact");
                response.EnsureSuccessStatusCode();
                var catFactResponse = await response.Content.ReadFromJsonAsync<CatFactResponse>();
                return catFactResponse?.Fact ?? "Could not fetch cat fact.";
            }
            catch (HttpRequestException ex)
            {
                // Log the exception (optional)
                Console.WriteLine($"Error fetching cat fact: {ex.Message}");
                return "Could not fetch cat fact due to network error.";
            }
            catch (Exception ex)
            {
                // Log the exception (optional)
                Console.WriteLine($"Unexpected error fetching cat fact: {ex.Message}");
                return "Could not fetch cat fact due to an unexpected error.";
            }
        }

        // Make public to be callable from Program.cs POST handler
        public async Task<string> GetWeatherDescriptionAsync(DateOnly date)
        {
            // Coordinates for Bangkok, Thailand
            double latitude = 13.7563;
            double longitude = 100.5018;
            string formattedDate = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

            // Construct the API URL - Updated timezone to Asia/Bangkok
            string requestUrl = $"v1/forecast?latitude={latitude}&longitude={longitude}&daily=weathercode,temperature_2m_max&timezone=Asia/Bangkok&start_date={formattedDate}&end_date={formattedDate}";

            try
            {
                var client = _httpClientFactory.CreateClient("WeatherClient");
                var response = await client.GetAsync(requestUrl);
                response.EnsureSuccessStatusCode();

                // Deserialize only the relevant 'daily' part
                var weatherData = await response.Content.ReadFromJsonAsync<OpenMeteoDailyResponse>();

                if (weatherData?.daily?.weathercode?.Length > 0 && weatherData.daily.temperature_2m_max?.Length > 0)
                {
                    int weatherCode = weatherData.daily.weathercode[0];
                    double maxTemp = weatherData.daily.temperature_2m_max[0];
                    string description = InterpretWeatherCode(weatherCode);
                    return $"{description}, Max Temp: {maxTemp}°C";
                }
                return "Weather data unavailable.";
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Error fetching weather data: {ex.Message}");
                return "Could not fetch weather due to network error.";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error fetching weather data: {ex.Message}");
                return "Could not fetch weather due to an unexpected error.";
            }
        }

        // Helper to interpret WMO Weather interpretation codes (from Open-Meteo docs)
        private string InterpretWeatherCode(int code)
        {
            return code switch
            {
                0 => "Clear sky",
                1 => "Mainly clear",
                2 => "Partly cloudy",
                3 => "Overcast",
                45 => "Fog",
                48 => "Depositing rime fog",
                51 => "Light drizzle",
                53 => "Moderate drizzle",
                55 => "Dense drizzle",
                56 => "Light freezing drizzle",
                57 => "Dense freezing drizzle",
                61 => "Slight rain",
                63 => "Moderate rain",
                65 => "Heavy rain",
                66 => "Light freezing rain",
                67 => "Heavy freezing rain",
                71 => "Slight snow fall",
                73 => "Moderate snow fall",
                75 => "Heavy snow fall",
                77 => "Snow grains",
                80 => "Slight rain showers",
                81 => "Moderate rain showers",
                82 => "Violent rain showers",
                85 => "Slight snow showers",
                86 => "Heavy snow showers",
                95 => "Thunderstorm", // Slight or moderate
                96 => "Thunderstorm with slight hail",
                99 => "Thunderstorm with heavy hail",
                _ => "Unknown weather code"
            };
        }
    }

    // Specific model for Open-Meteo daily response structure
    public class OpenMeteoDailyResponse
    {
        public DailyData? daily { get; set; }
    }

    public class DailyData
    {
        public string[]? time { get; set; }
        public int[]? weathercode { get; set; }
        public double[]? temperature_2m_max { get; set; }
    }
}
