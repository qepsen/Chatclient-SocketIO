using System;

public class DirectMessage : ChatMessage
{
    public string Content { get; set; } = "";
    public string Recipient { get; set; } = "";
    public bool IsOutgoing { get; set; }
    
    public override void Display()
    {
        var prefix = IsOutgoing ? $"DM to {Recipient}" : $"DM from {Sender}";
        Console.WriteLine($"[{Timestamp:HH:mm:ss}] [{prefix}]: {Content}");
    }
}