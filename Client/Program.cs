using System.Net.Sockets;
using System.Net;
using ChatStream;
using UpdateMGR;

namespace ChatClient;

public class Program
{
    private static Messenger inStream = new Messenger();
    private static Messenger outStream = new Messenger();
    private static TcpClient client;
    private static string username;
    private static bool isConnected = false;
    
    public static void Main(string[] args)
    {
        // Look for updates
        Updater updater = new Updater("KachleGG", "ChatApp", "1.0.3", "ChatClient");
        updater.Update();

        try
        {
            Console.WriteLine("Chat Client");
            Console.WriteLine("----------");
            
            // Get server IP address
            Console.Write("Enter server IP address: ");
            string? ipAddress = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(ipAddress) || !IPAddress.TryParse(ipAddress, out _))
            {
                Console.WriteLine("Invalid IP address.");
                return;
            }
            
            // Get server port
            Console.Write("Enter port: ");
            string? portInput = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(portInput) || !int.TryParse(portInput, out int serverPort))
            {
                Console.WriteLine("Invalid port number.");
                return;
            }
            
            // Get username
            Console.Write("Enter your username: ");
            username = Console.ReadLine()?.Trim() ?? "Anonymous";
            
            // Connect to server
            Console.WriteLine($"Connecting to {ipAddress}:{serverPort}...");
            client = new TcpClient();
            
            try
            {
                client.Connect(ipAddress, serverPort);
                isConnected = true;
                Console.WriteLine("Connected to server!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to connect: {ex.Message}");
                Console.WriteLine("If connection failed, make sure:");
                Console.WriteLine("1. The server is running");
                Console.WriteLine("2. The IP address is correct");
                Console.WriteLine("3. The port number matches the server's port");
                return;
            }
            
            Console.Clear();
            
            // Send username registration
            var registrationPacket = outStream.CreateMessagePacket(1, username);
            client.GetStream().Write(registrationPacket);
            
            // Start reading messages in a separate thread
            Thread readThread = new Thread(ReadPackets);
            readThread.IsBackground = true;
            readThread.Start();
            
            // Main loop for sending messages
            while (isConnected)
            {
                string? message = Console.ReadLine();
                
                if (string.IsNullOrEmpty(message)) continue;
                
                if (message.ToLower() == "exit")
                {
                    isConnected = false;
                    break;
                }
                
                try
                {
                    // Send the message
                    var packet = outStream.CreateMessagePacket(10, message);
                    client.GetStream().Write(packet);
                    
                    // Echo the message locally
                    Console.WriteLine($"You: {message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error sending message: {ex.Message}");
                    isConnected = false;
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
        finally
        {
            isConnected = false;
            client?.Close();
        }
    }
    
    private static void ReadPackets()
    {
        try
        {
            while (isConnected)
            {
                if (!client.Connected)
                {
                    Console.WriteLine("Disconnected from server.");
                    isConnected = false;
                    break;
                }
                
                NetworkStream stream = client.GetStream();
                
                if (stream.DataAvailable)
                {
                    byte[] buffer = new byte[client.ReceiveBufferSize];
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    
                    if (bytesRead == 0)
                    {
                        Console.WriteLine("Server closed the connection.");
                        isConnected = false;
                        break;
                    }
                    
                    // Parse the message
                    (int opcode, string message) = inStream.ParseMessagePacket(buffer.Take(bytesRead).ToArray());
                    
                    // Display the message based on opcode
                    if (opcode == 10) // Chat message
                    {
                        Console.WriteLine(message);
                    }
                    else if (opcode == 20) // System message
                    {
                        Console.WriteLine($"[System] {message}");
                    }
                    else
                    {
                        Console.WriteLine($"[{opcode}] {message}");
                    }
                }
                
                Thread.Sleep(10); // Small delay to prevent CPU overuse
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading messages: {ex.Message}");
            isConnected = false;
        }
    }
}