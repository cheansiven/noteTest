namespace Notes.Api.Controllers;

public static class RateLimitPolicies
{
    /// <summary>Applied to the sign-in and sign-up endpoints to blunt credential stuffing.</summary>
    public const string Authentication = "authentication";
}
