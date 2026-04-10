namespace POS.Infrastructure.Authentication;

public class JwtSettings
{
    public const string SectionName = "JwtSettings";
    public string Secret { get; init; } = null!;
    public int ExpiryHours { get; init; }
    public string Issuer { get; init; } = null!;
    public string Audience { get; init; } = null!;
}
