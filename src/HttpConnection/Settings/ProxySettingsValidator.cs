using XmlTypedSettings;

namespace HttpConnection.Settings;

public sealed class ProxySettingsValidator :
    ISettingsValidator<ProxySettings>
{
    /// <summary>
    /// Validates optional application proxy settings without constructing an HTTP client.
    /// A null value retains the platform's default proxy behavior.
    /// </summary>
    public static bool TryValidate(
        ProxySettings? settings,
        out string? error
        )
    {
        if (settings == null)
        {
            error = null;

            return true;
        }

        string[] errors = new ProxySettingsValidator().Validate(settings).ToArray();
        error = errors.Length == 0 ? null : string.Join(Environment.NewLine, errors);

        return errors.Length == 0;
    }

    public IEnumerable<string> Validate(ProxySettings settings)
    {
        if (!settings.UseProxy || settings.UseSystemProxy)
            yield break;

        if (string.IsNullOrWhiteSpace(settings.Host))
            yield return "Proxy host must be specified.";
        else if (Uri.CheckHostName(settings.Host) == UriHostNameType.Unknown)
            yield return "Proxy host must be a hostname or IP address without a scheme, " +
                "path, port or credentials.";

        if (settings.Port is < 1 or > 65535)
            yield return "Proxy port must be between 1 and 65535.";

        bool hasUser = !string.IsNullOrWhiteSpace(settings.User);
        bool hasPassword = !string.IsNullOrWhiteSpace(settings.Password);

        if (hasUser != hasPassword)
            yield return "Proxy username and password must be specified together.";
    }
}
