using System;
using System.Threading.Tasks;

class Program
{
    static async Task Main()
    {
        Console.WriteLine("=== Socket.IO Chatklient ===");
        
        string username;
        do
        {
            Console.Write("Ange ditt användarnamn: ");
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
                    Console.WriteLine("Hej då!");
                    return;
                
                case "/help":
                    ShowHelp();
                    break;
                
                case "/join":
                    if (!string.IsNullOrWhiteSpace(argument))
                        await chat.JoinRoom(argument);
                    else
                        Console.WriteLine("Användning: /join <rumnamn>");
                    break;
                
                case "/dm":
                    var dmParts = argument.Split(' ', 2);
                    if (dmParts.Length == 2)
                        await chat.SendDirectMessage(dmParts[0], dmParts[1]);
                    else
                        Console.WriteLine("Användning: /dm <användare> <meddelande>");
                    break;
                
                default:
                    Console.WriteLine("Okänt kommando. Skriv /help för hjälp.");
                    break;
            }
        }
    }

    static void ShowHelp()
    {
        Console.WriteLine("\nKommandon:");
        Console.WriteLine("  /help           - Visa kommandon");
        Console.WriteLine("  /join <rum>     - Byt rum");
        Console.WriteLine("  /dm <user> <text> - Skicka direktmeddelande");
        Console.WriteLine("  /quit           - Avsluta\n");
    }
}

