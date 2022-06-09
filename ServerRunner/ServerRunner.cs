using System.Net;
using CommonRunner;
using ServerClass;
int port;
string? nickname;

while (true)
{
    Console.WriteLine("포트를 입력해주세요");
    var givenPort = Console.ReadLine();
    Console.WriteLine("닉네임을 입력해주세요");
    nickname = Console.ReadLine();

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

using var serverObject = new Server(port);

Console.WriteLine("접속을 기다리고 있습니다...");

await serverObject.Accept();

Console.WriteLine("연결됐습니다!");

var log = new FileInfo($"{((IPEndPoint)serverObject.EndPoint).Address}" +
                       $"-{port}-{DateTimeOffset.Now:yyyyMMddHHmmss}.txt");
await using var logStream = log.OpenWrite();

var tasks = new List<Task>();

var printer = CommonRunnerMethods.PrintMessage(logStream, serverObject.GetStream, serverObject.ReadMessage);
var sender = CommonRunnerMethods.SendMessage(logStream, nickname, serverObject.WriteMessage);

tasks.Add(printer);
tasks.Add(sender);

await Task.WhenAll(tasks);