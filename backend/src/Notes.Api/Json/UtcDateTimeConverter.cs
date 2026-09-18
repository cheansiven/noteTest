using System.Text.Json;
using System.Text.Json.Serialization;

namespace Notes.Api.Json;

/// <summary>
/// Timestamps are stored as UTC in DATETIME2 columns, which come back from SQL Server with
/// <see cref="DateTimeKind.Unspecified"/>. Without this converter they would serialise with
/// no "Z" suffix and every browser would silently read them as local time.
/// </summary>
public sealed class UtcDateTimeConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        AsUtc(reader.GetDateTime());

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options) =>
        writer.WriteStringValue(AsUtc(value).ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'"));

    private static DateTime AsUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
    };
}
