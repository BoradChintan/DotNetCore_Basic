using System.Net.WebSockets;
using System.Text;

namespace DotNETBasic.Services
{
    public class WebSocketService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public WebSocketService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task HandleWebSocketAsync(WebSocket weatherWebSocket)
        {
            Console.WriteLine("Fetching weather updates...");
            while (weatherWebSocket.State == WebSocketState.Open)
            {
                // Fetch the actual weather data
                var weatherData = await GetWeatherData();

                // Ensure the data is not empty or null
                if (!string.IsNullOrEmpty(weatherData))
                {
                    // Send weather data to the WebSocket client
                    var weatherBuffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(weatherData), 0, weatherData.Length);
                    await weatherWebSocket.SendAsync(weatherBuffer, WebSocketMessageType.Text, true, CancellationToken.None);
                }

                await Task.Delay(10000);
            }
        }

        private async Task<string> GetWeatherData()
        {
            var client = _httpClientFactory.CreateClient();
            try
            {
                var response = await client.GetStringAsync("http://api.weatherapi.com/v1/current.json?key=ad8c5aee3684402eb3b81930242309&q=Ahmedabad&aqi=no");
                Console.WriteLine("Weather API Response: " + response);
                return response; // Return the weather data as a JSON string
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error fetching weather data: " + ex.Message);
                return $"Error fetching weather data: {ex.Message}";
            }
        }
    }
}
