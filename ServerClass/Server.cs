using System.Net;
using System.Net.Sockets;
using CommonNetwork;

namespace ServerClass;

public class Server : INetwork
{
    private readonly TcpListener _tcpListener;
    
    public Server(int port)
    {
        _tcpListener = new TcpListener(IPAddress.Any, port);
        _tcpListener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        _tcpListener.Start();
    }

    public TcpClient? TcpClient { get; private set; }

    public NetworkStream GetStream => TcpClient.GetStream();

    public EndPoint EndPoint => TcpClient.Client.RemoteEndPoint;

    public bool Connected => TcpClient.Connected;

    public void Dispose()
    {
        _tcpListener.Stop();
        _tcpListener.Server.Dispose();
    }

    public void Stop()
    {
        _tcpListener.Stop();
    }

    public async Task Accept()
    {
        TcpClient = await _tcpListener.AcceptTcpClientAsync();
    }

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