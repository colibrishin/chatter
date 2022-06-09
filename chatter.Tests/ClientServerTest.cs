using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using ClientClass;
using CommonRunner;
using ServerClass;

namespace chatter.Test;

public class ClientTest
{
    [SetUp]
    public void Setup()
    {
        
    }
    
    [Test]
    public async Task Start()
    {
        Server server = new Server(300);
        Client client = new Client(IPAddress.Parse("127.0.0.1"), 300);
        await client.Connect();
        await server.Accept();
        Assert.True(server.Connected);
        Assert.True(client.Connected);
        server.Stop();
        client.Stop();
    }

    [Test]
    public async Task ClientWriteMessage()
    {
        var server = new Server(300);
        var client = new Client(IPAddress.Parse("127.0.0.1"), 300);
        await client.Connect();
        await server.Accept();
        var message = Encoding.UTF8.GetBytes("test");
        
        Assert.IsNotNull(client.GetStream);
        await client.WriteMessage(message);
        var buffer = await server.ReadMessage();
        
        Assert.AreEqual(message.Length, buffer.Length);
        Assert.AreEqual(message, buffer);
        
        server.Stop();
        client.Stop();
    }
    
    [Test]
    public async Task ServerWriteMessage()
    {
        var server = new Server(400);
        var client = new Client(IPAddress.Parse("127.0.0.1"), 400);
        await client.Connect();
        await server.Accept();
        var message = Encoding.UTF8.GetBytes("test");
        
        Assert.IsNotNull(client.GetStream);
        await server.WriteMessage(message);
        var buffer = await client.ReadMessage();
        
        Assert.AreEqual(message.Length, buffer.Length);
        Assert.AreEqual(message, buffer);
        
        server.Stop();
        client.Stop();
    }

    [Test]
    public async Task SendFile()
    {
        var server = new Server(500);
        var client = new Client(IPAddress.Parse("127.0.0.1"), 500);
        await client.Connect();
        await server.Accept();

        var file = new FileInfo("C:\\test.jpg");
        var filestream = file.OpenRead();
        
        var originalData = new List<byte>(CommonRunnerMethods.PrefixFileTransfer);
        byte[] buffer = new byte[filestream.Length];
        _ = await filestream.ReadAsync(buffer.AsMemory(0, (int)filestream.Length));
        originalData.AddRange(buffer);

        await CommonRunnerMethods.SendFile(file.OpenRead(), client.WriteMessage);
        var received = new List<byte>();
        
        received.AddRange(await server.ReadMessage());

        while (server.GetStream.DataAvailable)
        {
            received.AddRange(await server.ReadMessage());
        }

        Assert.AreEqual(originalData, received);
    }

    [Test]
    public async Task ReceiveFile()
    {
        var server = new Server(500);
        var client = new Client(IPAddress.Parse("127.0.0.1"), 500);
        await client.Connect();
        await server.Accept();
        
        var file = new FileInfo("C:\\test.jpg");
        var filestream = file.OpenRead();
        var readFile = new byte[filestream.Length];
        _ = await filestream.ReadAsync(readFile);

        filestream.Seek(0, SeekOrigin.Begin);
        await CommonRunnerMethods.SendFile(filestream, client.WriteMessage);

        byte[] bytes = await server.ReadMessage();
        var filename = await CommonRunnerMethods.ReceiveFile(client.GetStream, bytes, client.ReadMessage);

        var receivedFile = File.OpenRead(filename);
        var receivedFileByte = new byte[receivedFile.Length];
        _ = await receivedFile.ReadAsync(receivedFileByte);
        
        Assert.NotNull(filename);
        Assert.AreEqual(readFile, receivedFileByte);
        Assert.True(File.Exists(filename));
    }
}