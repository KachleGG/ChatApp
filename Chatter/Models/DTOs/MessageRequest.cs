namespace Chatter.Models.DTOs;

public class MessageRequest
{
    public int UserId { get; set; }
    public string Message { get; set; }
    public int GroupId { get; set; } = 1; // Default to General group
}
