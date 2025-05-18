using System.Net.WebSockets;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseWebSockets();

// Test of removing 
CancellationTokenSource cts = new();

app.MapGet("/", () => "Hello World");

// Doesn't close the app, but terminates all connections. Might be useful :)
// this works as expected! Cleans all, then resets for more connections. 
app.MapGet("/shutdown", () => {
    cts.Cancel();
    Console.WriteLine("Cleared all current clients.");
    cts = new();
    return Results.Ok("Shutdown signal sent");
});

app.Map("/ws", async context => {
    if (context.WebSockets.IsWebSocketRequest) {
        using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        Console.WriteLine("Client connected.");

        var buffer = new byte[1024 * 4];
        var receiveTask = Task.Run(async () => {
            while (webSocket.State == WebSocketState.Open) {
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cts.Token);
                if (result.MessageType == WebSocketMessageType.Close) {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                    Console.WriteLine("Client disconnected.");
                    break;
                }

                var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                Console.WriteLine($"Received: {message}");
            }
        });

        var sendTask = Task.Run(async () => {
            while (webSocket.State == WebSocketState.Open) {
                var message = $"Server Time: {DateTime.UtcNow:HH:mm:ss}";
                var bytes = Encoding.UTF8.GetBytes(message);
                await webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
                await Task.Delay(1000);
            }
        });

        await Task.WhenAny(receiveTask, sendTask);
    } else {
        context.Response.StatusCode = 400;
    }
});


await app.RunAsync();


