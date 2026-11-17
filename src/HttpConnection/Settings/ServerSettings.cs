namespace HttpConnection.Settings
{
    public sealed class ServerSettings
    {
        public string Host { get; set; } = string.Empty;

        public int Port { get; set; }

        public string? ApiPath { get; set; }

        public bool IsSsl { get; set; }

        public int TimeOutSeconds { get; set; }

        public void CopyFrom(ServerSettings other)
        {
            this.Host = other.Host;
            this.Port = other.Port;
            this.ApiPath = other.ApiPath;
            this.IsSsl = other.IsSsl;
            this.TimeOutSeconds = other.TimeOutSeconds;
        }
    }
}
