namespace Chatter.Models;

public class Message
{
    public int Id { get; set; }
    public string Text { get; set; }
    public User SentFrom { get; set; }
}
