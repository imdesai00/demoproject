using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Npgsql;
using NpgsqlTypes;

namespace Common.Data;

public interface IPgFunctionExecutor
{
    Task<FnResult> CallAsync(string function, ActorContext actor, object? payload, CancellationToken ct);
}

/// <summary>
/// The only door to the database. Every write/read of business data goes through a named
/// Postgres function with signature (p_actor_id uuid, p_actor_role text, p_payload jsonb)
/// RETURNS jsonb, returning the {ok, code, message, data} envelope. No other SQL lives in C#.
/// </summary>
public sealed partial class PgFunctionExecutor : IPgFunctionExecutor
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly ILogger<PgFunctionExecutor> _logger;

    public PgFunctionExecutor(NpgsqlDataSource dataSource, ILogger<PgFunctionExecutor> logger)
    {
        _dataSource = dataSource;
        _logger = logger;
    }

    // The function name is interpolated into SQL (Postgres can't bind identifiers), so it MUST
    // come from a compile-time constant and pass this allow-list pattern: schema.fn_name.
    [GeneratedRegex(@"^[a-z_][a-z0-9_]*\.fn_[a-z0-9_]+$")]
    private static partial Regex SafeName();

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<FnResult> CallAsync(string function, ActorContext actor, object? payload, CancellationToken ct)
    {
        if (!SafeName().IsMatch(function))
        {
            throw new ArgumentException($"Illegal function name '{function}'.", nameof(function));
        }

        var body = JsonSerializer.Serialize(payload ?? new { }, JsonOptions);
        if (body.Length > 256 * 1024)
        {
            return new FnResult(false, "VALIDATION", "Payload too large", null);
        }

        await using var cmd = _dataSource.CreateCommand($"SELECT {function}($1, $2, $3)");
        cmd.CommandTimeout = 10;
        cmd.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Uuid, Value = actor.UserId });
        cmd.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text, Value = actor.Role });
        cmd.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Jsonb, Value = body });

        try
        {
            var raw = (string?)await cmd.ExecuteScalarAsync(ct);
            if (raw is null)
            {
                return new FnResult(false, "INTERNAL", "Empty response from database function", null);
            }

            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;
            return new FnResult(
                root.GetProperty("ok").GetBoolean(),
                root.TryGetProperty("code", out var c) ? c.GetString() ?? "INTERNAL" : "INTERNAL",
                root.TryGetProperty("message", out var m) ? m.GetString() : null,
                root.TryGetProperty("data", out var d) && d.ValueKind != JsonValueKind.Null ? d.Clone() : null);
        }
        catch (Exception ex) when (ex is NpgsqlException or JsonException or InvalidCastException)
        {
            _logger.LogError(ex, "DB function {Function} failed", function);
            return new FnResult(false, "INTERNAL", "Unexpected error", null);
        }
    }
}
