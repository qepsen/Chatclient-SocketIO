using System;

public abstract class ChatMessage
{
    public string Sender { get; set; } = "";
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public abstract void Display();
}