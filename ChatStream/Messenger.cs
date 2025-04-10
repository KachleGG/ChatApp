namespace ChatStream;

public class Messenger {

    // Create a byte array to store the message packet and send it to the server
    public byte[] CreateMessagePacket(int opcode, string message) {
        using MemoryStream ms = new MemoryStream();
        using BinaryWriter writer = new BinaryWriter(ms);

        // Write the opcode to the message packet
        writer.Write(opcode);
        writer.Write(message);

        // Return the message packet as a byte array
        return ms.ToArray();
    }

    // Create a byte array to store the message packet with username
    public byte[] CreateMessagePacketWithUsername(int opcode, string username, string message) {
        using MemoryStream ms = new MemoryStream();
        using BinaryWriter writer = new BinaryWriter(ms);

        // Write the opcode to the message packet
        writer.Write(opcode);
        writer.Write(username);
        writer.Write(message);

        // Return the message packet as a byte array
        return ms.ToArray();
    }

    // Parsing incoming data into opcode and message
    public (int opcode, string message) ParseMessagePacket(byte[] data) {
        using MemoryStream ms = new MemoryStream(data);
        using BinaryReader reader = new BinaryReader(ms);

        int opcode = reader.ReadInt32();
        string message = reader.ReadString();

        return (opcode, message);
    }

    // Parsing incoming data into opcode, username and message
    public (int opcode, string username, string message) ParseMessagePacketWithUsername(byte[] data) {
        using MemoryStream ms = new MemoryStream(data);
        using BinaryReader reader = new BinaryReader(ms);

        int opcode = reader.ReadInt32();
        string username = reader.ReadString();
        string message = reader.ReadString();

        return (opcode, username, message);
    }
}