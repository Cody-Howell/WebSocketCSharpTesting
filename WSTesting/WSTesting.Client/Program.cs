using System.Net.WebSockets;
using System.Text;

var client = new ClientWebSocket();
await client.ConnectAsync(new Uri("wss://localhost:5000/ws"), CancellationToken.None);
Console.WriteLine("Connected to server!");

var receiveTask = Task.Run(async () => {
    var buffer = new byte[1024 * 4];
    while (client.State == WebSocketState.Open) {
        var result = await client.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
        var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
        Console.WriteLine($"[Server] {message}");
    }
});

var sendTask = Task.Run(async () => {
    while (client.State == WebSocketState.Open) {
        var input = Console.ReadLine();
        if (input == "exit") break;

        var bytes = Encoding.UTF8.GetBytes(input);
        await client.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
    }

    await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client closing", CancellationToken.None);
});

await Task.WhenAny(receiveTask, sendTask);
