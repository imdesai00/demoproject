namespace Common.Data;

/// <summary>
/// The authenticated caller's identity, derived only from a validated JWT — never from a request body.
/// Role "system" is reserved for trusted internal callers (e.g. async event consumers) that bypass
/// per-user ownership filters inside a function; it is never attached to an inbound HTTP request.
/// </summary>
public sealed record ActorContext(Guid UserId, string Role)
{
    public static readonly ActorContext System = new(Guid.Empty, "system");
    public static readonly ActorContext Anonymous = new(Guid.Empty, "anonymous");
}
