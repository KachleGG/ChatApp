namespace Chatter.Models;

public class Message
{
    public ulong Id { get; set; }
    public string Text { get; set; }
    public User SentFrom { get; set; }
}
