namespace Chatter.Models.DTOs;

public class CreateInviteRequest
{
    // 0 = unlimited
    public int MaxUses { get; set; } = 1;

    // seconds until expiry from now (optional)
    public int? ExpiresInSeconds { get; set; }

    public string? Note { get; set; }
}
