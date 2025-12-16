using System;

public class TextMessage : ChatMessage
{
    public string Content { get; set; } = "";
    
    public override void Display() =>
        Console.WriteLine($"[{Timestamp:HH:mm:ss}] {Sender}: {Content}");
}