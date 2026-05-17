namespace Shiny.Push;

/// <summary>
/// Result of a push permission request; pairs the permission outcome with the resulting registration token when granted.
/// </summary>
/// <param name="Status">The permission/access state after the request.</param>
/// <param name="RegistrationToken">The registration token issued by the push provider when access was granted; otherwise null.</param>
public record PushAccessState(
    AccessState Status,
    string? RegistrationToken
)
{
    /// <summary>
    /// Cached denied result with no registration token.
    /// </summary>
    public static PushAccessState Denied { get; } = new PushAccessState(AccessState.Denied, null);

    /// <summary>
    /// Throws <see cref="PermissionException"/> when the current status is not <see cref="AccessState.Available"/>.
    /// </summary>
    public void Assert()
    {
        if (this.Status != AccessState.Available)
            throw new PermissionException("Push registration fail", this.Status);
    }
}
