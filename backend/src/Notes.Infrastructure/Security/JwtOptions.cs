using System.ComponentModel.DataAnnotations;

namespace Notes.Infrastructure.Security;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required]
    public string Issuer { get; set; } = string.Empty;

    [Required]
    public string Audience { get; set; } = string.Empty;

    /// <summary>HS256 signing key. Must be at least 256 bits to satisfy the algorithm.</summary>
    [Required, MinLength(32)]
    public string Secret { get; set; } = string.Empty;

    [Range(1, 60 * 24 * 30)]
    public int ExpiryMinutes { get; set; } = 720;
}
