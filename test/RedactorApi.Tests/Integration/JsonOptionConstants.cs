using System.Text.Json;

namespace RedactorApi.Tests.Integration;

public static class JsonOptionConstants
{
    public static JsonSerializerOptions SerializerOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };
}