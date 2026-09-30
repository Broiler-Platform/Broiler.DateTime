// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   3
// Annotated:        3/3
// Exempt:           0
// Human-reviewed:   0/3
// IP risk:          Low
// Security risk:    Critical
// Criteria:         3/2
// Resource impact:  3/10 max
// Unverified:       3
//
// GENERATED - DO NOT EDIT MANUALLY

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Broiler.DateTime;

/// <summary>
/// A <see cref="System.Text.Json"/> converter that reads and writes <see cref="ExtendedIsoDateTime"/>
/// values as ISO-8601 / RFC-3339 extended strings (the same form produced by
/// <see cref="ExtendedIsoDateTime.ToStringIso"/>).
/// </summary>
// Broiler-AI:           Origin=AI; IP=Low; Security=Critical; Resources=3; Fingerprint=64C3BB
// Broiler-Falsified-If: a JSON string token longer than 64 bytes reaches the stackalloc in ExtendedIsoDateTime.TryParse(ReadOnlySpan<byte>) instead of failing its length guard
// Broiler-Human:        PENDING
public sealed class ExtendedIsoDateTimeJsonConverter : JsonConverter<ExtendedIsoDateTime>
{
    /// <inheritdoc />
    // Broiler-AI:           Origin=AI; IP=Low; Security=Critical; Resources=3; Fingerprint=11DF97
    // Broiler-Falsified-If: a string token whose raw ValueSpan exceeds 64 bytes is copied into a stack buffer sized from it by the UTF-8 TryParse fast path
    // Broiler-Human:        PENDING
    public override ExtendedIsoDateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException(
                $"Expected a string when reading {nameof(ExtendedIsoDateTime)} but found {reader.TokenType}.");

        if (!reader.HasValueSequence)
        {
            ReadOnlySpan<byte> span = reader.ValueSpan;
            if (ExtendedIsoDateTime.TryParse(span, null, out ExtendedIsoDateTime? parsed))
                return parsed;
        }

        string? text = reader.GetString();
        if (!ExtendedIsoDateTime.TryParse(text, out ExtendedIsoDateTime? value))
            throw new JsonException($"'{text}' is not a valid ISO-8601 extended date-time.");

        return value;
    }

    /// <inheritdoc />
    // Broiler-AI:           Origin=AI; IP=Low; Security=Low; Resources=1; Fingerprint=0A06CF
    // Broiler-Falsified-If: a value whose ISO form does not fit the 64-character stack buffer is written truncated instead of through the ToStringIso fallback
    // Broiler-Human:        PENDING
    public override void Write(Utf8JsonWriter writer, ExtendedIsoDateTime? value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        Span<char> buffer = stackalloc char[64];
        if (value.TryFormat(buffer, out int charsWritten))
        {
            writer.WriteStringValue(buffer[..charsWritten]);
        }
        else
        {
            writer.WriteStringValue(value.ToStringIso());
        }
    }
}
