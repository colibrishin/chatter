using System.Net.Sockets;

namespace CommonNetwork;

public interface INetwork : IDisposable
{
    public TcpClient TcpClient { get; }
    
    public bool Connected { get; }
    
    public NetworkStream GetStream { get; }
    
    public Task WriteMessage(ReadOnlyMemory<byte> message);
    
    public Task<byte[]> ReadMessage();

    public void Stop();
}