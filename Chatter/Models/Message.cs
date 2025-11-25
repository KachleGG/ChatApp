namespace Chatter.Models;

public class Message
{
    public int Id { get; set; }
    public string Text { get; set; }
    public User SentFrom { get; set; }
    // UTC timestamp for when the message was sent
    public DateTime SentAt { get; set; }
}
