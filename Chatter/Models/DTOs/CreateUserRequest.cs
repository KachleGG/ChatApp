namespace Chatter.Models.DTOs;

public class CreateUserRequest
{
    public string Name { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
    // Optional invite code used when server is in private mode
    public string? InviteCode { get; set; }
}
