using System.Text.Json;
using System.Text.Json.Serialization;

namespace Broiler.DateTime;

/// <summary>
/// A <see cref="System.Text.Json"/> converter that reads and writes <see cref="ExtendedIsoDateTime"/>
/// values as ISO-8601 / RFC-3339 extended strings (the same form produced by
/// <see cref="ExtendedIsoDateTime.ToStringIso"/>).
/// </summary>
public sealed class ExtendedIsoDateTimeJsonConverter : JsonConverter<ExtendedIsoDateTime>
{
    /// <inheritdoc />
    public override ExtendedIsoDateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException(
                $"Expected a string when reading {nameof(ExtendedIsoDateTime)} but found {reader.TokenType}.");

        string? text = reader.GetString();
        if (!ExtendedIsoDateTime.TryParse(text, out ExtendedIsoDateTime? value))
            throw new JsonException($"'{text}' is not a valid ISO-8601 extended date-time.");

        return value;
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, ExtendedIsoDateTime value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStringValue(value.ToStringIso());
    }
}
