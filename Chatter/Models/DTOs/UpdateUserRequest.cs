namespace Chatter.Models.DTOs;

public class UpdateUserRequest
{
    public string? Name { get; set; }
    public string? Email { get; set; }
    // Optional new password
    public string? Password { get; set; }
    // Required when changing password: the user's current password (plain text for verification)
    public string? CurrentPassword { get; set; }
}
