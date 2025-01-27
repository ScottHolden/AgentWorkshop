public class WeatherService
{
    public Task<double> GetTemperature(string location)
        => Task.FromResult(Random.Shared.NextDouble() * 20 + 10);
}