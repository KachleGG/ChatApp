using System.Net;

namespace ChatStream;

public class Constants {
    // Port number for the chat server
    public static int PORT = 8910;

    // Using .Any to accept connections from any IP address
    public static IPAddress Address = IPAddress.Any;

    // Using .Loopback for local testing only
    //public static IPAddress Address = IPAddress.Loopback;
}
