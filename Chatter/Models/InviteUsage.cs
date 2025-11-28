namespace Chatter.Models;

public class InviteUsage
{
    public int Id { get; set; }
    public int InviteId { get; set; }
    public Invite Invite { get; set; } = null!;

    // User created using the invite
    public int UserId { get; set; }

    public DateTime UsedAt { get; set; }

    public string? SourceIp { get; set; }
}
