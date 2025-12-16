using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using SocketIOClient;

public class SocketManager
{
    private readonly SocketIOClient.SocketIO socket;
    private readonly string username;
    private string currentRoom = "general";
    private readonly List<ChatMessage> messageHistory = new();
    private const string HistoryFile = "chat_history.json";
    
    private const string MessageEvent = "ludwigs_message";
    private const string DirectMessageEvent = "direct_message";
    private const string JoinRoomEvent = "join_room";
    private const string UserJoinedEvent = "user_joined";
    private const string UserLeftEvent = "user_left";

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

        socket.On(MessageEvent, response =>
        {
            var data = response.GetValue<MessageData>();
            var message = new TextMessage { Sender = data.sender, Content = data.message };
            DisplayAndSave(message);
        });

        socket.On(DirectMessageEvent, response =>
        {
            var data = response.GetValue<DirectMessageData>();
            
            if (data.to == username || data.from == username)
            {
                var message = new DirectMessage 
                { 
                    Sender = data.from,
                    Recipient = data.to,
                    Content = data.message,
                    IsOutgoing = data.from == username
                };
                DisplayAndSave(message);
            }
        });

        socket.On(UserJoinedEvent, response =>
        {
            var data = response.GetValue<UserEventData>();
            Console.WriteLine($"→ {data.username} joined {data.room}");
        });

        socket.On(UserLeftEvent, response =>
        {
            var data = response.GetValue<UserEventData>();
            Console.WriteLine($"← {data.username} left {data.room}");
        });
    }

    private void DisplayAndSave(ChatMessage message)
    {
        message.Display();
        messageHistory.Add(message);
        SaveHistory();
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
        var localMessage = new TextMessage { Sender = username, Content = message };
        DisplayAndSave(localMessage);
        
        await socket.EmitAsync(MessageEvent, new
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
        return socket.EmitAsync(JoinRoomEvent, new { username, room, previousRoom });
    }

    public async Task SendDirectMessage(string recipient, string message)
    {
        await socket.EmitAsync(DirectMessageEvent, new { from = username, to = recipient, message });
        
        var dm = new DirectMessage 
        { 
            Sender = username,
            Recipient = recipient,
            Content = message,
            IsOutgoing = true
        };
        dm.Display();
    }

    public void ShowHistory(int count = 10)
    {
        Console.WriteLine($"\nLast {count} messages");
        var start = Math.Max(0, messageHistory.Count - count);
        var messages = messageHistory.GetRange(start, messageHistory.Count - start);
        
        foreach (var msg in messages)
            msg.Display();
        Console.WriteLine();
    }

    private void SaveHistory()
    {
        try
        {
            var json = JsonSerializer.Serialize(messageHistory, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(HistoryFile, json);
        }
        catch { }
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
        catch { }
    }

    private record MessageData(string sender, string message, string room);
    private record DirectMessageData(string from, string to, string message);
    private record UserEventData(string username, string room);
}
