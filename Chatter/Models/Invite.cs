using System.ComponentModel.DataAnnotations;

namespace Chatter.Models;

public class Invite
{
    public int Id { get; set; }

    // Short, unique invite code (upper-case). Example: "ABCD1234"
    public string Code { get; set; } = null!;

    public int CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }

    // 0 = unlimited
    public int MaxUses { get; set; }

    // current usage count
    public int UsesCount { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public bool IsRevoked { get; set; }

    public string? Note { get; set; }

    [Timestamp]
    public byte[]? RowVersion { get; set; }
}
