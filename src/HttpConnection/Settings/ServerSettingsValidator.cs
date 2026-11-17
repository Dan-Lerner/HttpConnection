using XmlTypedSettings;

namespace HttpConnection.Settings
{
    public sealed class ServerSettingsValidator :
        ISettingsValidator<ServerSettings>
    {
        public IEnumerable<string> Validate(ServerSettings settings)
        {
            if (string.IsNullOrWhiteSpace(settings.Host))
                yield return "Server host must be specified.";
            else if (Uri.CheckHostName(settings.Host) == UriHostNameType.Unknown)
                yield return "Server host must be a valid DNS name or IP address.";

            if (settings.Port is < 1 or > 65535)
                yield return "Server port must be between 1 and 65535.";

            if (!string.IsNullOrWhiteSpace(settings.ApiPath) &&
                Uri.TryCreate(settings.ApiPath, UriKind.Absolute, out _))
            {
                yield return "Server API path must be relative.";
            }
        }
    }
}
