using System.Security.Claims;

namespace Notes.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Reads the subject without throwing, for callers that must cope with a token
    /// that does not carry a usable id.
    /// </summary>
    public static bool TryGetUserId(this ClaimsPrincipal principal, out Guid userId)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub");
        return Guid.TryParse(value, out userId);
    }

    /// <summary>
    /// Reads the authenticated user's id from the bearer token. Actions are [Authorize]d,
    /// so a missing or unparsable subject means the pipeline is misconfigured - a bug,
    /// not bad user input.
    /// </summary>
    public static Guid GetUserId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub");

        return Guid.TryParse(value, out var id)
            ? id
            : throw new InvalidOperationException("The authenticated principal has no valid user id claim.");
    }
}
