using System.ComponentModel;
using System.Text.Json;

namespace RichStokoe.AgentTools.Utils;

public static class LocationTools
{
    private static readonly HttpClient _httpClient = new();

    [AgentTool]
    [Description("Gets the geographic location from an IP address. Returns city, region, country, and coordinates.")]
    public static async Task<string> Get_Location_From_Ip_Address(
        [Description("The IP address to look up. You can use the Get)Public_Ip_Address tool to get the curent user's public IP first.")] string ipAddress)
    {
        try
        {
            // Using ip-api.com - free for non-commercial use, no API key required
            var response = await _httpClient.GetStringAsync($"http://ip-api.com/json/{ipAddress}");
            var json = JsonDocument.Parse(response);
            var root = json.RootElement;

            if (root.GetProperty("status").GetString() == "fail")
            {
                return $"Failed to lookup IP: {root.GetProperty("message").GetString()}";
            }

            var city = root.GetProperty("city").GetString();
            var region = root.GetProperty("regionName").GetString();
            var country = root.GetProperty("country").GetString();
            var lat = root.GetProperty("lat").GetDouble();
            var lon = root.GetProperty("lon").GetDouble();
            var timezone = root.GetProperty("timezone").GetString();
            var isp = root.GetProperty("isp").GetString();

            return $"Location: {city}, {region}, {country}\nCoordinates: {lat}, {lon}\nTimezone: {timezone}\nISP: {isp}";
        }
        catch (Exception ex)
        {
            return $"Error fetching location: {ex.Message}";
        }
    }
}
