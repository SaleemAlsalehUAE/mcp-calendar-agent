using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Newtonsoft.Json.Linq;
using System;
using System.ComponentModel;
using System.Net.Http;
using System.Threading.Tasks;

public class WeatherPlugin
{
    private readonly string _apiKey;

    // The constructor takes our configuration so it can read the secret API key
    public WeatherPlugin(IConfiguration config)
    {
        _apiKey = config["OpenWeatherMap:ApiKey"] ?? throw new InvalidOperationException("OpenWeatherMap API key not configured.");
    }

    [KernelFunction, Description("Gets the current weather for a specified city.")]
    public async Task<string> GetCurrentWeather(
        [Description("The name of the city (e.g., San Francisco, CA).")] string location
    )
    {
        Console.WriteLine($"--- TOOL CALLED: GetCurrentWeather for {location} ---");
        using var client = new HttpClient();
        var url = $"https://api.openweathermap.org/data/2.5/weather?q={location}&appid={_apiKey}&units=metric";

        try
        {
            var response = await client.GetStringAsync(url);
            var data = JObject.Parse(response);
            var temp = data["main"]["temp"];
            var description = data["weather"][0]["description"];
            var result = $"The current weather in {location} is {temp}°C with {description}.";
            Console.WriteLine($"--- TOOL SUCCEEDED: {result} ---");
            return result;
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"--- TOOL FAILED: {ex.Message} ---");
            Console.ResetColor();
            return $"Could not retrieve weather data for {location}. Error: {ex.Message}";
        }
    }
}