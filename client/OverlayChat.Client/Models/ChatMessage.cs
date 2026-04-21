namespace OverlayChat.Client.Models;

public sealed class ChatMessage
{
    public string Type { get; set; } = "chat";
    public string Room { get; set; } = "default";
    public string Name { get; set; } = "anon";
    public string Text { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
