using System.ComponentModel.DataAnnotations;

namespace HttpConnection.OAuth;

public sealed class OAuthOptions
{
    public const string ServiceSectionKey = "OAuth";

    [Required, Url]
    public string Authority { get; init; } = "";

    [Required]
    public string ClientId { get; init; } = "";

    [Required, Url]
    public string RedirectUri { get; init; } = "";

    [Required]
    public string Scopes { get; init; } = "";

    [Range(10, 600)]
    public int CallbackTimeoutSeconds { get; init; } = 120;

    [Range(0, 300)]
    public int AccessTokenSafetyWindowSeconds { get; init; } = 60;
}
