using System.Net;
using System.Net.Sockets;
using ClientClass;
using CommonRunner;

while (true)
{
    IPAddress? address;
    int port;
    string? nickname;
    while (true)
    {
        Console.WriteLine("주소를 입력해주세요");
        var givenAddress = Console.ReadLine();
        Console.WriteLine("포트를 입력해주세요");
        var givenPort = Console.ReadLine();
        Console.WriteLine("닉네임을 입력해주세요");
        nickname = Console.ReadLine();

        if (givenAddress is null || !IPAddress.TryParse(givenAddress, out address))
        {
            Console.WriteLine("주소를 잘못 입력하셨습니다.");
            continue;
        }

        if (givenPort is null || !int.TryParse(givenPort, out port) || port <= 0)
        {
            Console.WriteLine("포트를 잘못 입력하셨습니다.");
            continue;
        }

        if (nickname is null)
        {
            Console.WriteLine("닉네임은 빈칸일 수 없습니다.");
            continue;
        }

        break;
    }

    try
    {
        using var clientObject = new Client(address, port);

        Console.WriteLine("접속을 시도합니다...");
        
        await clientObject.Connect();
        
        Console.WriteLine("연결됐습니다!");
        
        var log = new FileInfo($"{address}" +
                               $"-{port}-{DateTimeOffset.Now:yyyyMMddHHmmss}.txt");
        await using var logStream = log.OpenWrite();

        var tasks = new List<Task>();

        var printer = CommonRunnerMethods.PrintMessage(logStream, clientObject.GetStream, clientObject.ReadMessage);
        var sender = CommonRunnerMethods.SendMessage(logStream, nickname, clientObject.WriteMessage);

        tasks.Add(printer);
        tasks.Add(sender);

        await Task.WhenAll(tasks);

    }
    catch (SocketException)
    {
        Console.WriteLine("연결에 실패했습니다. 정보를 확인하고 다시 시도해 주세요!");
    }
}
