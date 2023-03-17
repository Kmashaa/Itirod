using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;


int localPort = 0;
bool isAdmin = false;
bool isAllowed = false;
using var sender = new UdpClient(AddressFamily.InterNetwork);
IPAddress brodcastAddress = IPAddress.Parse("235.5.5.11");
Console.Write("Login: ");
string? username = Console.ReadLine();
Console.WriteLine("Enter 'create' or 'join'");

while (localPort == 0)
{
    string? message = Console.ReadLine();

        try
        {
            var words = message.Split(' ');
            switch (words[0])
            {
                case "create":
                    Console.WriteLine("Enter number of room");
                    string? num = Console.ReadLine();
                    localPort = Int32.Parse(num);
                    if (System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties().GetActiveUdpListeners().Any(p => p.Port == localPort))
                    {
                        Console.WriteLine("Room already exists. Try again.\nEnter 'create' or 'join'");
                        localPort = 0;
                        continue;
                    }
                    isAdmin = true;
                    isAllowed = true;
                    Console.WriteLine("Room was created");
                    break;
                case "join":
                    Console.WriteLine("Enter number of room");
                    string? numm = Console.ReadLine();
                    localPort = Int32.Parse(numm);
                    if (!System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties().GetActiveUdpListeners().Any(p => p.Port == localPort))
                    {
                        Console.WriteLine("Room does not exist. Try again.\nEnter 'create' or 'join'");
                        localPort = 0;
                        continue;
                    }
                    byte[] data = Encoding.UTF8.GetBytes($"*{username} wants to join your room: 'allow' or 'prohibit'");
                    await sender.SendAsync(data, new IPEndPoint(brodcastAddress, localPort));
                    await Task.Run(ReceiveMessageAsync);
                    break;
                default:
                    Console.WriteLine("Try again");
                    continue;
            }
        }
        catch (Exception)
        {
            Console.WriteLine("Error. Try again. \nEnter 'create' or 'join'");
        }
}

Task.Run(ReceiveMessageAsync);
await SendMessageAsync();

async Task SendMessageAsync()
{
    while (true)
    {
        byte[] data;
        string? message = Console.ReadLine();
        if (isAdmin)
        {
            switch (message)
            {
                case "allow":
                    data = Encoding.UTF8.GetBytes(message);
                    await sender.SendAsync(data, new IPEndPoint(brodcastAddress, localPort));
                    continue;
                case "prohibit":
                    data = Encoding.UTF8.GetBytes(message);
                    await sender.SendAsync(data, new IPEndPoint(brodcastAddress, localPort));
                    continue;
                default:
                    Console.WriteLine("Undefined command");
                    continue;
            }
        }
        message = $"{username}: {message}";
        data = Encoding.UTF8.GetBytes(message);
        Console.SetCursorPosition(0, Console.CursorTop - 1);
        await sender.SendAsync(data, new IPEndPoint(brodcastAddress, localPort));
    }
}

async Task ReceiveMessageAsync()
{
    using var receiver = new UdpClient();
    receiver.ExclusiveAddressUse = false;
    receiver.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
    receiver.Client.Bind(new IPEndPoint(IPAddress.Any, localPort));
    byte[] membershipAddresses = new byte[12];
    Buffer.BlockCopy(brodcastAddress.GetAddressBytes(), 0, membershipAddresses, 0, 4);
    Buffer.BlockCopy(IPAddress.Any.GetAddressBytes(), 0, membershipAddresses, 4, 4);
    Buffer.BlockCopy(IPAddress.Any.GetAddressBytes(), 0, membershipAddresses, 8, 4);
    receiver.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, membershipAddresses);
    //receiver.MulticastLoopback = false;
    while (true)
    {
        byte[] data;
        var result = await receiver.ReceiveAsync();
        string message = Encoding.UTF8.GetString(result.Buffer);
        if (!isAllowed && message.Equals("allow"))
        {
            Console.WriteLine("Welcome");
            data = Encoding.UTF8.GetBytes($"{username} joins the room");
            await sender.SendAsync(data, new IPEndPoint(brodcastAddress, localPort));
            isAllowed = true;
            return;
        }
        else if (!isAllowed && message.Equals("prohibit"))
        {
            Console.WriteLine("Access denied");
            System.Environment.Exit(1);
            return;
        }
        else if (message.StartsWith('*') && !isAdmin || !isAllowed || message.Equals("allow") || message.Equals("prohibit"))
        {
            continue;
        }
        Console.WriteLine(message);
    }
}