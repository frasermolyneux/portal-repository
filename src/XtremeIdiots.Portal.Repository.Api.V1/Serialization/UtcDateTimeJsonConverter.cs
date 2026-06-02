using Newtonsoft.Json;
using System.Globalization;

namespace XtremeIdiots.Portal.Repository.Api.V1.Serialization;

/// <summary>
/// Ensures DateTime values are serialized as UTC with explicit offset information.
/// </summary>
public sealed class UtcDateTimeJsonConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(DateTime) || objectType == typeof(DateTime?);
    }

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
        {
            if (objectType == typeof(DateTime?))
            {
                return null;
            }

            return default(DateTime);
        }

        if (reader.TokenType == JsonToken.Date)
        {
            var rawDate = reader.Value switch
            {
                DateTime dt => dt,
                DateTimeOffset dto => dto.UtcDateTime,
                string dateText when DateTimeOffset.TryParse(
                    dateText,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out var parsedDateOffset) => parsedDateOffset.UtcDateTime,
                _ => throw new JsonSerializationException("Invalid DateTime value.")
            };

            return NormalizeToUtc(rawDate);
        }

        var text = reader.Value?.ToString();
        if (!string.IsNullOrWhiteSpace(text)
            && DateTimeOffset.TryParse(
                text,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var parsedOffset))
        {
            return NormalizeToUtc(parsedOffset.UtcDateTime);
        }

        throw new JsonSerializationException($"Invalid DateTime string: '{text}'.");
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value is null)
        {
            writer.WriteNull();
            return;
        }

        var dateTime = value switch
        {
            DateTime dt => dt,
            _ => throw new JsonSerializationException($"Unsupported DateTime value type: {value.GetType().FullName}")
        };

        var utcDate = NormalizeToUtc(dateTime);
        writer.WriteValue(utcDate.ToString("o"));
    }

    private static DateTime NormalizeToUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Unspecified => DateTime.SpecifyKind(value, DateTimeKind.Utc),
            _ => value.ToUniversalTime()
        };
    }
}
