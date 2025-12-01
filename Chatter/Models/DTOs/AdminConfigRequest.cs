namespace Chatter.Models.DTOs;

public class AdminConfigRequest
{
    public bool? PrivateMode { get; set; }
    public bool? ProhibitGroups { get; set; }
    public bool? ProhibitGeneral { get; set; }
    public int? UserGroupLimit { get; set; }
    public bool? BackupEnabled { get; set; }
    public string? BackupSchedule { get; set; }
    public string? BackupPath { get; set; }
    public int? BackupRetention { get; set; }
    public string? HttpUrl { get; set; }
    public string? HttpsUrl { get; set; }
}
