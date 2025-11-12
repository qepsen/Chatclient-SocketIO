using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using SocketIOClient;

public abstract class ChatMessage
{
    public string Sender { get; set; } = "";
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public abstract void Display();
}

public class TextMessage : ChatMessage
{
    public string Content { get; set; } = "";
    
    public override void Display() =>
        Console.WriteLine($"[{Timestamp:HH:mm:ss}] {Sender}: {Content}");
}

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

public class SocketManager
{
    private readonly SocketIOClient.SocketIO socket;
    private readonly string username;
    private string currentRoom = "general";
    private readonly List<ChatMessage> messageHistory = new();
    private const string HistoryFile = "chat_history.json";

    public SocketManager(string username)
    {
        this.username = username;
        socket = new SocketIOClient.SocketIO("wss://api.leetcode.se", new SocketIOOptions
        {
            Path = "/sys25d"
        });
        LoadHistory();
        SetupEventHandlers();
    }

    private void SetupEventHandlers()
    {
        socket.OnConnected += async (sender, e) =>
        {
            Console.WriteLine("Connected to server!");
            await Task.Delay(500);
            await JoinRoom(currentRoom);
        };

        socket.OnDisconnected += (sender, e) => 
            Console.WriteLine("Disconnected from server");

        socket.On("ludwigs_message", response =>
        {
            var data = response.GetValue<MessageData>();
            var message = new TextMessage 
            { 
                Sender = data.sender, 
                Content = data.message
            };
            message.Display();
            messageHistory.Add(message);
            SaveHistory();
        });

        socket.On("direct_message", response =>
        {
            var data = response.GetValue<DirectMessageData>();
            var message = new DirectMessage 
            { 
                Sender = data.from,
                Recipient = data.to,
                Content = data.message,
                IsOutgoing = false
            };
            message.Display();
            messageHistory.Add(message);
            SaveHistory();
        });

        socket.On("user_joined", response =>
        {
            var data = response.GetValue<UserEventData>();
            Console.WriteLine($"→ {data.username} joined {data.room}");
        });

        socket.On("user_left", response =>
        {
            var data = response.GetValue<UserEventData>();
            Console.WriteLine($"← {data.username} left {data.room}");
        });
    }

    public async Task Connect()
    {
        Console.WriteLine("Connecting...");
        await socket.ConnectAsync();
        await Task.Delay(1000);
    }

    public Task Disconnect()
    {
        SaveHistory();
        return socket.DisconnectAsync();
    }

    public async Task SendMessage(string message)
    {
        var localMessage = new TextMessage
        {
            Sender = username,
            Content = message
        };
        localMessage.Display();
        messageHistory.Add(localMessage);
        SaveHistory();
        
        await socket.EmitAsync("ludwigs_message", new
        {
            sender = username,
            message = message,
            room = currentRoom
        });
    }

    public Task JoinRoom(string room)
    {
        var previousRoom = currentRoom;
        currentRoom = room;
        Console.WriteLine($"→ You are now in room: {room}");
        return socket.EmitAsync("join_room", new { username, room, previousRoom });
    }

    public async Task SendDirectMessage(string recipient, string message)
    {
        var dm = new DirectMessage 
        { 
            Sender = username,
            Recipient = recipient,
            Content = message,
            IsOutgoing = true
        };
        dm.Display();
        messageHistory.Add(dm);
        SaveHistory();
        
        await socket.EmitAsync("direct_message", new { from = username, to = recipient, message });
    }

    public void ShowHistory(int count = 10)
    {
        Console.WriteLine($"\n--- Last {count} messages ---");
        var recent = messageHistory.Count > count 
            ? messageHistory.GetRange(messageHistory.Count - count, count)
            : messageHistory;
        
        foreach (var msg in recent)
            msg.Display();
        Console.WriteLine();
    }

    private void SaveHistory()
    {
        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(messageHistory, options);
            File.WriteAllText(HistoryFile, json);
        }
        catch
        {
            
        }
    }

    private void LoadHistory()
    {
        try
        {
            if (File.Exists(HistoryFile))
            {
                var json = File.ReadAllText(HistoryFile);
                var loaded = JsonSerializer.Deserialize<List<ChatMessage>>(json);
                if (loaded != null)
                    messageHistory.AddRange(loaded);
            }
        }
        catch
        {
            
        }
    }
    
    private record MessageData(string sender, string message, string room);
    private record DirectMessageData(string from, string to, string message);
    private record UserEventData(string username, string room);
}