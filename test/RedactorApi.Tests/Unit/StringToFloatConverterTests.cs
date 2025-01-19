using System;
using System.Text.Json;
using RedactorApi.Analyzer.Converters;
using Xunit;

namespace RedactorApi.Tests.Unit;

public class StringToFloatConverterTests
{
    private readonly JsonSerializerOptions _options = new()
    {
        Converters = { new StringToFloatConverter() }
    };

    [Fact]
    public void Read_ValidFloatString_ReturnsFloat()
    {
        var json = "\"123.45\"";
        var result = JsonSerializer.Deserialize<float>(json, _options);
        Assert.Equal(123.45f, result);
    }

    [Fact]
    public void Read_InvalidFloatString_ThrowsJsonException()
    {
        var json = "\"invalid\"";
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<float>(json, _options));
    }

    [Fact]
    public void Read_ValidFloatNumber_ReturnsFloat()
    {
        var json = "123.45";
        var result = JsonSerializer.Deserialize<float>(json, _options);
        Assert.Equal(123.45f, result);
    }

    [Fact]
    public void Write_FloatValue_WritesNumber()
    {
        var value = 123.45f;
        var json = JsonSerializer.Serialize(value, _options);
        Assert.Equal("123.45", json);
    }
}