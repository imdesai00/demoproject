using Common.Data;
using Identity.Api.Models.Entities;

namespace Identity.Api.Repositories;

public interface IUserRepository
{
    Task<FnResult> RegisterAsync(string email, string passwordHash, string displayName, CancellationToken ct);
    Task<FnResult> FindForLoginAsync(string email, CancellationToken ct);
}

public class UserRepository : IUserRepository
{
    private readonly IPgFunctionExecutor _db;

    public UserRepository(IPgFunctionExecutor db)
    {
        _db = db;
    }

    public Task<FnResult> RegisterAsync(string email, string passwordHash, string displayName, CancellationToken ct) =>
        _db.CallAsync("identity.fn_register", ActorContext.Anonymous,
            new { email, password_hash = passwordHash, display_name = displayName }, ct);

    public Task<FnResult> FindForLoginAsync(string email, CancellationToken ct) =>
        _db.CallAsync("identity.fn_login_lookup", ActorContext.Anonymous, new { email }, ct);

    public static UserRecord MapUser(System.Text.Json.JsonElement data) => new(
        data.GetGuid("id"),
        data.GetString("email"),
        data.GetString("display_name"),
        data.GetNullableString("password_hash"));
}
