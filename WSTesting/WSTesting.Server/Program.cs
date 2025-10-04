using System.Net.WebSockets;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<Chat>();

// Add services to the container.

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseWebSockets();

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseRouting();

app.MapGet("/hi", () => "Hello World");

app.MapGet("/chat", (Chat chat) => chat.messages);
app.MapGet("/chat/{user}", (string user, string message, Chat chat) => {
    var m = new ChatMessage { user = user, message = message };
    chat.AddMessage(m);
    return Results.Ok();
});

// Keep a thread-safe set of connected WebSocket clients
var sockets = new System.Collections.Concurrent.ConcurrentDictionary<string, WebSocket>();

// Subscribe to new chat messages and broadcast to all connected sockets
var chat = app.Services.GetRequiredService<Chat>();
chat.MessageAdded += async (ChatMessage m) => {
    var json = System.Text.Json.JsonSerializer.Serialize(m);
    var bytes = Encoding.UTF8.GetBytes(json);
    var segment = new ArraySegment<byte>(bytes);

    var tasks = new List<System.Threading.Tasks.Task>();
    foreach (var kv in sockets) {
        var ws = kv.Value;
        if (ws.State == WebSocketState.Open) {
            try {
                tasks.Add(ws.SendAsync(segment, WebSocketMessageType.Text, true, CancellationToken.None));
            } catch {
                // ignore send errors; cleanup will happen elsewhere
            }
        }
    }

    try {
        await Task.WhenAll(tasks);
    } catch {
        // swallowing exceptions for now
    }
};

app.Map("/ws", async context => {
    if (context.WebSockets.IsWebSocketRequest) {
        var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        var id = Guid.NewGuid().ToString();
        sockets.TryAdd(id, webSocket);
        Console.WriteLine("WebSocket connected: " + id);

        var buffer = new byte[1024 * 4];
        WebSocketReceiveResult? result = null;

        try {
            do {
                result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), default);
                if (result.MessageType == WebSocketMessageType.Close) break;

                var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                Console.WriteLine($"Received from {id}: {message}");

                // Echo back or optionally parse and add to chat
                // var response = Encoding.UTF8.GetBytes($"Server echo: {message}");
                // await webSocket.SendAsync(new ArraySegment<byte>(response), WebSocketMessageType.Text, true, default);

            } while (!(result?.CloseStatus.HasValue ?? false));
        } catch (Exception ex) {
            Console.WriteLine("WebSocket error: " + ex.Message);
        }

        // Clean up
        sockets.TryRemove(id, out var _);
        if (webSocket.State != WebSocketState.Closed) {
            try {
                await webSocket.CloseAsync(result?.CloseStatus ?? WebSocketCloseStatus.NormalClosure, result?.CloseStatusDescription, default);
            } catch { }
        }

        Console.WriteLine("WebSocket closed: " + id);
    } else {
        context.Response.StatusCode = 400;
    }
});

app.MapFallbackToFile("index.html");


await app.RunAsync();


