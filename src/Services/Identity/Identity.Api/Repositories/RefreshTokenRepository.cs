using Common.Data;
using Identity.Api.Models.Entities;

namespace Identity.Api.Repositories;

public interface IRefreshTokenRepository
{
    Task<FnResult> CreateAsync(Guid userId, string tokenHash, DateTime expiresAt, CancellationToken ct);
    Task<FnResult> RotateAsync(string oldTokenHash, string newTokenHash, DateTime newExpiresAt, CancellationToken ct);
    Task<FnResult> RevokeAsync(string tokenHash, CancellationToken ct);
}

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly IPgFunctionExecutor _db;

    public RefreshTokenRepository(IPgFunctionExecutor db)
    {
        _db = db;
    }

    public Task<FnResult> CreateAsync(Guid userId, string tokenHash, DateTime expiresAt, CancellationToken ct) =>
        _db.CallAsync("identity.fn_create_refresh_token", ActorContext.Anonymous,
            new { user_id = userId, token_hash = tokenHash, expires_at = expiresAt }, ct);

    public Task<FnResult> RotateAsync(string oldTokenHash, string newTokenHash, DateTime newExpiresAt, CancellationToken ct) =>
        _db.CallAsync("identity.fn_rotate_refresh_token", ActorContext.Anonymous,
            new { old_token_hash = oldTokenHash, new_token_hash = newTokenHash, new_expires_at = newExpiresAt }, ct);

    public Task<FnResult> RevokeAsync(string tokenHash, CancellationToken ct) =>
        _db.CallAsync("identity.fn_revoke_refresh_token", ActorContext.Anonymous, new { token_hash = tokenHash }, ct);

    public static UserRecord MapUser(System.Text.Json.JsonElement data) => new(
        data.GetGuid("id"), data.GetString("email"), data.GetString("display_name"));
}
