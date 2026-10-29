using System.Text.Json;

namespace Common.Data;

/// <summary>
/// Result envelope every Postgres function returns: { ok, code, message, data }.
/// </summary>
public sealed record FnResult(bool Ok, string Code, string? Message, JsonElement? Data);
