namespace HttpConnection.Settings;

public sealed class ProxySettings
{
    /// <summary>
    /// Enables proxy routing; false connects directly to the target server.
    /// </summary>
    public bool UseProxy { get; set; }

    /// <summary>
    /// Uses the operating system proxy and current Windows credentials when proxy routing is enabled.
    /// </summary>
    public bool UseSystemProxy { get; set; }

    /// <summary>
    /// Specifies the explicit proxy hostname or IP address without a scheme, path, port or credentials.
    /// </summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>
    /// Specifies the explicit proxy TCP port, from 1 through 65535.
    /// </summary>
    public int Port { get; set; }

    /// <summary>
    /// Specifies the optional explicit proxy username; set together with Password.
    /// </summary>
    public string? User { get; set; }

    /// <summary>
    /// Specifies the optional explicit proxy password; set together with User and never log this value.
    /// </summary>
    public string? Password { get; set; }

    public void CopyFrom(ProxySettings other)
    {
        UseProxy = other.UseProxy;
        UseSystemProxy = other.UseSystemProxy;
        Host = other.Host;
        Port = other.Port;
        User = other.User;  
        Password = other.Password;
    }
}
