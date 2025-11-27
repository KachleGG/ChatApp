namespace Chatter.Models;

public class Group
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int OwnerId { get; set; }
    public User Owner { get; set; } = null!;
    public bool IsDeactivated { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
