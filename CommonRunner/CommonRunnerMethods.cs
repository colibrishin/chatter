// See https://aka.ms/new-console-template for more information

using System.Text;
using System;
using System.Net.Sockets;
using FileTypeChecker;

namespace CommonRunner;

public static class CommonRunnerMethods
{
    public delegate Task<byte[]> DelegateReadMessage();

    public delegate Task DelegateSendMessage(ReadOnlyMemory<byte> message);

    internal static readonly byte[] PrefixFileTransfer = { 0x2f, 0x66, 0x74, 0x2f, };

    public static async Task PrintMessage(
        FileStream logStream,
        NetworkStream networkStream,
        DelegateReadMessage messageLoader)
    {
        while (true)
        {
            var byteData = await messageLoader();
            var decodedData = Encoding.UTF8.GetString(byteData);
            if (decodedData.StartsWith("/ft/"))
            {
                await ReceiveFile(networkStream, byteData, messageLoader);
            }
            else
            {
                await MarshalChatLog(logStream, decodedData);
                Console.WriteLine($"---> {decodedData}");
            }
        }
    }
    
    public static async Task SendMessage(FileStream logStream, string nickname, DelegateSendMessage messageSender)
    {
        while (true)
        {
            var message = Console.ReadLine();
            
            if (message == null) continue;

            if (message.StartsWith("/ft/"))
            {
                var dir = message.Skip(4).ToArray();
                try
                {
                    await using var fileStream = new FileInfo(new string(dir)).OpenRead();
                    await SendFile(fileStream, messageSender);
                }
                catch
                {
                    Console.WriteLine("Failed To open file.");
                    continue;
                }
            }
            else
            {
                message = $"{nickname} : {message}";
                await MarshalChatLog(logStream, message);
                await messageSender(Encoding.UTF8.GetBytes(message));
            }
        }
    }

    private static async Task MarshalChatLog(FileStream fileStream, string message)
    {
        var buffer = Encoding.UTF8.GetBytes(message + '\n');
        fileStream.Seek(0, SeekOrigin.End);
        await fileStream.WriteAsync(buffer);
    }

    internal static async Task<string?> ReceiveFile(
        NetworkStream networkStream,
        byte[] byteData,
        DelegateReadMessage messageLoader)
    {
        var fileBytes = new List<byte>(byteData.Skip(4).ToArray());
        
        while (networkStream.DataAvailable)
        {
            fileBytes.AddRange(await messageLoader());
        }

        var filename = $"{DateTimeOffset.UtcNow:yyyyMMddHHmmssZ}";
        try
        {
            var file = new FileInfo(filename);
            await using var fileStream = file.OpenWrite();
            await fileStream.WriteAsync(fileBytes.ToArray());
            fileStream.Close();
            
            await using var fileStream2 = file.OpenRead();
            var fileType = FileTypeValidator.GetFileType(fileStream2);
            var oldFilename = filename;
            var extension = fileType is null ? "txt" : fileType.Extension;
            filename = filename + '.' + extension;
            fileStream2.Close();
            
            try
            {
                File.Move(oldFilename, filename);
            }
            catch
            {
                Console.WriteLine("Failed to finalize the file receiving");
            }

            Console.WriteLine($"File {filename} received.");
            return filename;
        }
        catch
        {
            Console.WriteLine("Failed to open file.");
        }

        return null;
    }

    internal static async Task SendFile(FileStream fileStream, DelegateSendMessage messageSender)
    {
        const int bufferSizeWithoutPrefix = 65531;
        const int bufferSize = 65535;
        
        long offset = 0;
        long fullCount = fileStream.Length;
        if (fullCount >= Int32.MaxValue)
        {
            Console.WriteLine("Filesize limit reached.");
            return;
        }
        
        long count = fullCount > bufferSizeWithoutPrefix ? bufferSizeWithoutPrefix : fullCount;

        byte[] buffer = new byte[bufferSize];
        var readBytes = await fileStream.ReadAsync(buffer.AsMemory(0, (int)count));

        var merged = new List<byte>(PrefixFileTransfer);
        merged.AddRange(buffer);

        // LINQ Take does not applied to ReadOnlyMemory.
        await messageSender(
            merged.Take(readBytes == bufferSizeWithoutPrefix ? bufferSize : readBytes + 4).ToArray());
        fullCount -= count;
        offset += readBytes;
        count = fullCount < bufferSize ? fullCount : bufferSize;

        while (fullCount > 0)
        {
            merged.Clear();
            Console.WriteLine($"Sending... {offset}/{fileStream.Length}");
            fileStream.Seek(offset, SeekOrigin.Begin);
            readBytes = await fileStream.ReadAsync(buffer, 0, (int)count);
            merged.AddRange(buffer.Take((int)count));
            await messageSender(merged.ToArray());
            fullCount -= count;
            offset += readBytes;
            count = fullCount > bufferSize ? bufferSize : fullCount;
        }
        Console.WriteLine($"File sent.");
    }
}