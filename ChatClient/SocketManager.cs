using System;
using System.Threading.Tasks;
using SocketIOClient;

public class SocketManager
{
    private readonly SocketIOClient.SocketIO socket;
    private readonly string username;
    private string currentRoom = "general";

    public SocketManager(string username)
    {
        this.username = username;
        socket = new SocketIOClient.SocketIO("wss://api.leetcode.se", new SocketIOOptions
        {
            Path = "/sys25d"
        });

        SetupEventHandlers();
    }

    private void SetupEventHandlers()
    {
        socket.OnConnected += async (sender, e) =>
        {
            Console.WriteLine("✓ Ansluten till servern!");
            await JoinRoom(currentRoom);
        };

        socket.OnDisconnected += (sender, e) => 
            Console.WriteLine("✗ Frånkopplad från servern");

        socket.On("ludwigs_message", response =>
        {
            var data = response.GetValue<MessageData>();
            PrintMessage(data.sender, data.message);
        });

        socket.On("direct_message", response =>
        {
            var data = response.GetValue<DirectMessageData>();
            PrintMessage($"DM från {data.from}", data.message);
        });

        socket.On("user_joined", response =>
        {
            var data = response.GetValue<UserEventData>();
            Console.WriteLine($"→ {data.username} anslöt till {data.room}");
        });

        socket.On("user_left", response =>
        {
            var data = response.GetValue<UserEventData>();
            Console.WriteLine($"← {data.username} lämnade {data.room}");
        });
    }

    private void PrintMessage(string sender, string message) =>
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {sender}: {message}");

    public Task Connect() => socket.ConnectAsync();

    public Task Disconnect() => socket.DisconnectAsync();

    public Task SendMessage(string message) =>
        socket.EmitAsync("ludwigs_message", new { sender = username, message, room = currentRoom });

    public Task JoinRoom(string room)
    {
        var previousRoom = currentRoom;
        currentRoom = room;
        Console.WriteLine($"→ Du är nu i rummet: {room}");
        return socket.EmitAsync("join_room", new { username, room, previousRoom });
    }

    public Task SendDirectMessage(string recipient, string message)
    {
        PrintMessage($"DM till {recipient}", message);
        return socket.EmitAsync("direct_message", new { from = username, to = recipient, message });
    }
    
    private record MessageData(string sender, string message, string room);
    private record DirectMessageData(string from, string to, string message);
    private record UserEventData(string username, string room);
}