namespace BookingService.Database;

public static class EnvironmentVariables
{
    public static string GetRequiredVariable(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        return string.IsNullOrEmpty(value) ? throw new InvalidOperationException($"Required environment variable {name} is not set.") : value;
    }
}