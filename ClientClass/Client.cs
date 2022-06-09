using System.Net;
using System.Net.Sockets;
using CommonNetwork;

namespace ClientClass;

public class Client : INetwork
{
    private readonly IPAddress _address;
    private readonly int _port;

    public Client(IPAddress address, int port)
    {
        TcpClient = new TcpClient();
        TcpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        _address = address;
        _port = port;
    }

    public TcpClient TcpClient { get; private set; }

    public bool Connected => TcpClient.Connected;

    public void Dispose()
    {
        TcpClient.Close();
        TcpClient.Client.Dispose();
    }

    public void Stop()
    {
        TcpClient.Close();
    }

    public async Task Connect()
    {
        await TcpClient.ConnectAsync(_address, _port);
    }

    public NetworkStream GetStream => TcpClient.GetStream();

    public async Task WriteMessage(ReadOnlyMemory<byte> message)
    {
        await GetStream.WriteAsync(message);
    }

    public async Task<byte[]> ReadMessage()
    {
        byte[] buffer = new byte[65535];
        var readBytes = await GetStream.ReadAsync(buffer);
        return buffer.Take(readBytes).ToArray();
    }
}