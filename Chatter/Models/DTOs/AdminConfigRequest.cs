namespace Chatter.Models.DTOs;

public class AdminConfigRequest
{
    // null means "do not change"
    public bool? ProhibitGroups { get; set; }
    public bool? PrivateMode { get; set; }
    public bool? ProhibitGeneral { get; set; }
    // Optional Kestrel URLs, e.g. "http://*:9090" or "https://*:9443"
    public string? HttpUrl { get; set; }
    public string? HttpsUrl { get; set; }
}
