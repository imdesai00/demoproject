namespace Identity.Api.Models.Entities;

/// <summary>Internal shape of an identity.users row — never serialized back to clients directly.</summary>
public record UserRecord(Guid Id, string Email, string DisplayName, string? PasswordHash = null);
