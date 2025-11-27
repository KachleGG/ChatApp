namespace Chatter.Models.DTOs;

public class AdminConfigRequest
{
    public bool? PrivateMode { get; set; }
    public bool? ProhibitGroups { get; set; }
    public bool? ProhibitGeneral { get; set; }
    public string? HttpUrl { get; set; }
    public string? HttpsUrl { get; set; }
}
