using System.Net.Sockets;
using System.Diagnostics;
using System.IO;
using ChatStream;
using UpdateMGR;

namespace ChatServer;

public class Program
{
    private static Messenger inStream = new Messenger();
    private static Messenger outStream = new Messenger();
    private static Dictionary<TcpClient, string> clients = new Dictionary<TcpClient, string>();
    private static TcpListener listener;
    private static string logFilePath;

    public static void Main(string[] args)
    {
        // Look for updates
        Updater updater = new Updater("KachleGG", "ChatApp", "1.0.3", "ChatServer");
        updater.Update();

        try
        {
            // Set log file path to the same directory as the executable
            logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs.txt");
            
            // Append server start message to the log file
            File.AppendAllText(logFilePath, $"\nChat Server Log - Started at {DateTime.Now}\n");
            
            listener = new TcpListener(Constants.Address, Constants.PORT);
            listener.Start();

            Console.WriteLine($"Server started on {Constants.Address}:{Constants.PORT}");
            Console.WriteLine("Waiting for connections...");
            Console.WriteLine("Press Ctrl+C to stop the server");
            Console.WriteLine($"Logging to: {logFilePath}");
            
            LogToFile($"Server started on {Constants.Address}:{Constants.PORT}");

            while (true)
            {
                AcceptClients();
                ReceiveMessages();
                Thread.Sleep(10); // Small delay to prevent CPU overuse
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Server error: {ex.Message}");
            LogToFile($"Server error: {ex.Message}");
        }
        finally
        {
            // Clean up resources
            foreach (var client in clients.Keys)
            {
                try
                {
                    client.Close();
                }
                catch { }
            }
            
            listener?.Stop();
        }
    }

    private static void AcceptClients()
    {
        try
        {
            for (int i = 0; i < 5; i++)
            {
                if (!listener.Pending()) continue;

                var client = listener.AcceptTcpClient();
                string clientAddress = client.Client.RemoteEndPoint.ToString();
                Console.WriteLine($"Client connected: {clientAddress}");
                LogToFile($"Client connected: {clientAddress}");
                
                // Add client with temporary username
                clients.Add(client, "Unknown");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error accepting clients: {ex.Message}");
            LogToFile($"Error accepting clients: {ex.Message}");
        }
    }

    private static void ReceiveMessages()
    {
        // Create a copy of the clients list to avoid modification during enumeration
        var clientsCopy = new Dictionary<TcpClient, string>(clients);
        
        foreach (var client in clientsCopy.Keys)
        {
            try
            {
                if (!client.Connected)
                {
                    string username = clients[client];
                    clients.Remove(client);
                    Console.WriteLine($"Client disconnected: {username} ({client.Client.RemoteEndPoint})");
                    LogToFile($"Client disconnected: {username} ({client.Client.RemoteEndPoint})");
                    continue;
                }

                NetworkStream stream = client.GetStream();

                if (stream.DataAvailable)
                {
                    byte[] buffer = new byte[client.ReceiveBufferSize];
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    
                    if (bytesRead == 0)
                    {
                        // Client disconnected
                        string username = clients[client];
                        clients.Remove(client);
                        client.Close();
                        Console.WriteLine($"Client disconnected: {username} ({client.Client.RemoteEndPoint})");
                        LogToFile($"Client disconnected: {username} ({client.Client.RemoteEndPoint})");
                        continue;
                    }
                    
                    // Parse the message
                    (int opcode, string message) = inStream.ParseMessagePacket(buffer.Take(bytesRead).ToArray());
                    
                    // Handle username registration (opcode 1)
                    if (opcode == 1)
                    {
                        string oldUsername = clients[client];
                        clients[client] = message;
                        Console.WriteLine($"User registered: {message} ({client.Client.RemoteEndPoint})");
                        LogToFile($"User registered: {message} ({client.Client.RemoteEndPoint})");
                        
                        // Notify all clients about the new user
                        BroadcastSystemMessage($"User {message} has joined the chat.");
                    }
                    // Handle chat messages (opcode 10)
                    else if (opcode == 10)
                    {
                        string username = clients[client];
                        Console.WriteLine($"{username}: {message}");
                        LogToFile($"{username}: {message}");
                        
                        // Broadcast the message to all clients
                        Broadcast(client, username, message);
                    }
                    else
                    {
                        Console.WriteLine($"Received: [{opcode}] - {message}");
                        LogToFile($"Received: [{opcode}] - {message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing client {client.Client.RemoteEndPoint}: {ex.Message}");
                LogToFile($"Error processing client {client.Client.RemoteEndPoint}: {ex.Message}");
                try
                {
                    string username = clients[client];
                    clients.Remove(client);
                    client.Close();
                    Console.WriteLine($"Client disconnected due to error: {username} ({client.Client.RemoteEndPoint})");
                    LogToFile($"Client disconnected due to error: {username} ({client.Client.RemoteEndPoint})");
                }
                catch { }
            }
        }
    }

    private static void Broadcast(TcpClient sender, string username, string message)
    {
        var clientsCopy = new Dictionary<TcpClient, string>(clients);
        
        foreach (var client in clientsCopy.Keys.Where(c => c != sender))
        {
            try
            {
                if (!client.Connected) continue;
                
                // Create a message packet with the username and message
                var packet = outStream.CreateMessagePacket(10, $"{username}: {message}");
                client.GetStream().Write(packet);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error broadcasting to client {client.Client.RemoteEndPoint}: {ex.Message}");
                LogToFile($"Error broadcasting to client {client.Client.RemoteEndPoint}: {ex.Message}");
                try
                {
                    string clientUsername = clients[client];
                    clients.Remove(client);
                    client.Close();
                    Console.WriteLine($"Client disconnected due to broadcast error: {clientUsername} ({client.Client.RemoteEndPoint})");
                    LogToFile($"Client disconnected due to broadcast error: {clientUsername} ({client.Client.RemoteEndPoint})");
                }
                catch { }
            }
        }
    }
    
    private static void BroadcastSystemMessage(string message)
    {
        var clientsCopy = new Dictionary<TcpClient, string>(clients);
        
        foreach (var client in clientsCopy.Keys)
        {
            try
            {
                if (!client.Connected) continue;
                
                var packet = outStream.CreateMessagePacket(20, message);
                client.GetStream().Write(packet);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error broadcasting system message to client {client.Client.RemoteEndPoint}: {ex.Message}");
                LogToFile($"Error broadcasting system message to client {client.Client.RemoteEndPoint}: {ex.Message}");
            }
        }
    }
    
    private static void LogToFile(string message)
    {
        try
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            File.AppendAllText(logFilePath, $"[{timestamp}] {message}\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error writing to log file: {ex.Message}");
        }
    }
}