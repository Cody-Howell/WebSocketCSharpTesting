

public class Chat {
    public List<ChatMessage> messages { get; set; } = new();
    public event Action<ChatMessage>? MessageAdded;

    public void AddMessage(ChatMessage m) {
        m.timestamp = DateTime.Now;
        messages.Add(m);
        MessageAdded?.Invoke(m);
    }

}

public class ChatMessage {
    public string message { get; set; } = "Unknown message";
    public string user { get; set; } = "Unknown user";
    public DateTime timestamp { get; set; }
}