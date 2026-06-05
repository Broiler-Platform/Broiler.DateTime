using System.Text.Json;
using Broiler.DateTime;
using Xunit;

namespace Broiler.DateTime.Tests;

public class JsonTests
{
    private sealed record Event(string Name, ExtendedIsoDateTime When);

    [Fact]
    public void Serializes_to_iso_string()
    {
        var v = ExtendedIsoDateTime.Parse("2025-06-05T14:30:00.123Z");
        string json = JsonSerializer.Serialize(v);
        Assert.Equal("\"2025-06-05T14:30:00.123Z\"", json);
    }

    [Fact]
    public void Deserializes_from_iso_string()
    {
        var v = JsonSerializer.Deserialize<ExtendedIsoDateTime>("\"+010000-01-01T00:00:00Z\"");
        Assert.NotNull(v);
        Assert.Equal(10000, v!.Year);
    }

    [Fact]
    public void Round_trips_through_property()
    {
        var e = new Event("launch", ExtendedIsoDateTime.Parse("-000001-01-01T00:00:00Z"));
        string json = JsonSerializer.Serialize(e);
        var back = JsonSerializer.Deserialize<Event>(json);
        Assert.NotNull(back);
        Assert.True(e.When.EqualsExact(back!.When));
    }

    [Fact]
    public void Null_round_trips()
    {
        ExtendedIsoDateTime? v = JsonSerializer.Deserialize<ExtendedIsoDateTime>("null");
        Assert.Null(v);
        Assert.Equal("null", JsonSerializer.Serialize<ExtendedIsoDateTime?>(null));
    }

    [Fact]
    public void Invalid_string_throws_JsonException()
    {
        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<ExtendedIsoDateTime>("\"not-a-date\""));
    }

    [Fact]
    public void Non_string_token_throws_JsonException()
    {
        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<ExtendedIsoDateTime>("12345"));
    }
}
