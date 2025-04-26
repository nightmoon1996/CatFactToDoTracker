namespace TodoList.Models
{
    // Simplified model for Open-Meteo current weather response
    public class WeatherResponse
    {
        public CurrentWeather? current_weather { get; set; }
    }

    public class CurrentWeather
    {
        public double temperature { get; set; }
        public int weathercode { get; set; }
        // Add other properties if needed (e.g., windspeed)
    }
}
