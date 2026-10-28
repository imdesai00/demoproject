using System.Text.Json;

namespace Common.Data;

/// <summary>
/// Typed readers for a Postgres function's "data" JSON payload — the ADO.NET-world equivalent
/// of a DataReader extension, kept in one place instead of copy-pasted into every repository.
/// </summary>
public static class JsonElementExtensions
{
    public static Guid GetGuid(this JsonElement element, string property) =>
        Guid.Parse(element.GetProperty(property).GetString()!);

    public static Guid? GetNullableGuid(this JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out var value) || value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }
        return Guid.Parse(value.GetString()!);
    }

    public static string GetString(this JsonElement element, string property) =>
        element.GetProperty(property).GetString()!;

    public static string? GetNullableString(this JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out var value) || value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }
        return value.GetString();
    }

    public static DateTime GetDateTime(this JsonElement element, string property) =>
        element.GetProperty(property).GetDateTime();

    public static DateTime? GetNullableDateTime(this JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out var value) || value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }
        return value.GetDateTime();
    }

    public static DateOnly? GetNullableDateOnly(this JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out var value) || value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }
        return DateOnly.FromDateTime(value.GetDateTime());
    }

    public static int GetInt(this JsonElement element, string property) =>
        element.GetProperty(property).GetInt32();

    public static List<JsonElement> GetArray(this JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.Array
            ? [.. value.EnumerateArray()]
            : [];
}
