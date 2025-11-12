using System;
using System.Threading.Tasks;

class Program
{
    static async Task Main()
    {
        Console.WriteLine("ChatClient");
        
        string username;
        do
        {
            Console.Write("Enter your username: ");
            username = Console.ReadLine()?.Trim();
        } while (string.IsNullOrWhiteSpace(username));

        var chat = new SocketManager(username);
        await chat.Connect();

        ShowHelp();
        
        while (true)
        {
            string input = Console.ReadLine()?.Trim();
            if (string.IsNullOrWhiteSpace(input)) continue;
            
            if (!input.StartsWith("/"))
            {
                await chat.SendMessage(input);
                continue;
            }
            
            var parts = input.Split(' ', 2);
            var command = parts[0].ToLower();
            var argument = parts.Length > 1 ? parts[1] : "";

            switch (command)
            {
                case "/quit":
                    await chat.Disconnect();
                    Console.WriteLine("Goodbye!");
                    return;
                
                case "/help":
                    ShowHelp();
                    break;
                
                case "/join":
                    if (!string.IsNullOrWhiteSpace(argument))
                        await chat.JoinRoom(argument);
                    else
                        Console.WriteLine("Usage: /join <roomname>");
                    break;
                
                case "/dm":
                    var dmParts = argument.Split(' ', 2);
                    if (dmParts.Length == 2)
                        await chat.SendDirectMessage(dmParts[0], dmParts[1]);
                    else
                        Console.WriteLine("Usage: /dm <user> <message>");
                    break;
                
                case "/history":
                    int count = 10;
                    if (!string.IsNullOrWhiteSpace(argument) && int.TryParse(argument, out int parsed))
                        count = parsed;
                    chat.ShowHistory(count);
                    break;
                
                default:
                    Console.WriteLine("Unknown command. Type /help for help.");
                    break;
            }
        }
    }

    static void ShowHelp()
    {
        Console.WriteLine("\nCommands:");
        Console.WriteLine("  /help              - Show commands");
        Console.WriteLine("  /join <room>       - Switch room");
        Console.WriteLine("  /dm <user> <msg>   - Send direct message");
        Console.WriteLine("  /history [count]   - Show history (default 10)");
        Console.WriteLine("  /quit              - Exit\n");
    }
}

