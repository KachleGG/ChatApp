namespace Chatter.Models;

public class AppSettings
{
    public ServerSettings? ServerSettings { get; set; }
    public LoggingSettings? Logging { get; set; }
    public string? AllowedHosts { get; set; }
    public KestrelSettings? Kestrel { get; set; }
}

public class LoggingSettings
{
    public Dictionary<string, string>? LogLevel { get; set; }
}

public class ServerSettings
{
    public bool PrivateMode { get; set; } = false;
    public bool ProhibitGroups { get; set; } = false;
}

public class KestrelSettings
{
    public KestrelEndpoints? Endpoints { get; set; }
}

public class KestrelEndpoints
{
    public KestrelEndpoint? Http { get; set; }
    public KestrelEndpoint? Https { get; set; }
}

public class KestrelEndpoint
{
    public string? Url { get; set; }
}
