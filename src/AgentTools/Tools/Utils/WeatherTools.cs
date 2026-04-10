using System.ComponentModel;
using System.Text.Json;

namespace RichStokoe.AgentTools.Utils;

public static class WeatherTools
{
    private static readonly HttpClient _httpClient = new();

    [AgentTool]
    [Description("Get the current weather for a given location. Returns temperature, conditions, humidity, and wind information.")]
    public static async Task<string> Get_Current_Weather_For_Location(
        [Description("The location to get weather for. Can be a city name (e.g., 'London') or 'latitude,longitude' coordinates.")] string location)
    {
        try
        {
            // Using wttr.in - free weather service, no API key required
            // Format=j1 returns JSON format
            var encodedLocation = Uri.EscapeDataString(location);
            var response = await _httpClient.GetStringAsync($"https://wttr.in/{encodedLocation}?format=j1");
            var json = JsonDocument.Parse(response);
            var root = json.RootElement;

            var currentCondition = root.GetProperty("current_condition")[0];
            var nearestArea = root.GetProperty("nearest_area")[0];

            var areaName = nearestArea.GetProperty("areaName")[0].GetProperty("value").GetString();
            var country = nearestArea.GetProperty("country")[0].GetProperty("value").GetString();
            var tempC = currentCondition.GetProperty("temp_C").GetString();
            var tempF = currentCondition.GetProperty("temp_F").GetString();
            var feelsLikeC = currentCondition.GetProperty("FeelsLikeC").GetString();
            var weatherDesc = currentCondition.GetProperty("weatherDesc")[0].GetProperty("value").GetString();
            var humidity = currentCondition.GetProperty("humidity").GetString();
            var windSpeedKmph = currentCondition.GetProperty("windspeedKmph").GetString();
            var windDir = currentCondition.GetProperty("winddir16Point").GetString();
            var visibility = currentCondition.GetProperty("visibility").GetString();
            var uvIndex = currentCondition.GetProperty("uvIndex").GetString();

            return $"""
                Weather for {areaName}, {country}
                Temperature: {tempC}°C ({tempF}°F), feels like {feelsLikeC}°C
                Conditions: {weatherDesc}
                Humidity: {humidity}%
                Wind: {windSpeedKmph} km/h {windDir}
                Visibility: {visibility} km
                UV Index: {uvIndex}
                """;
        }
        catch (Exception ex)
        {
            return $"Error fetching weather: {ex.Message}";
        }
    }
}
