using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using OverlayChat.Client.Models;

namespace OverlayChat.Client.Services;

public sealed class ChatWebSocketClient
{
    private readonly ClientWebSocket _socket = new();
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public async Task ConnectAsync(Uri serverUri, CancellationToken cancellationToken)
    {
        if (_socket.State == WebSocketState.Open)
        {
            return;
        }

        await _socket.ConnectAsync(serverUri, cancellationToken);
    }

    public async Task SendChatAsync(string text, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Serialize(new { text });
        var bytes = Encoding.UTF8.GetBytes(payload);
        await _socket.SendAsync(bytes, WebSocketMessageType.Text, true, cancellationToken);
    }

    public async Task ReceiveLoopAsync(Func<ChatMessage, Task> onMessage, CancellationToken cancellationToken)
    {
        var buffer = new byte[4096];

        while (!cancellationToken.IsCancellationRequested && _socket.State == WebSocketState.Open)
        {
            using var ms = new MemoryStream();
            WebSocketReceiveResult result;

            do
            {
                result = await _socket.ReceiveAsync(buffer, cancellationToken);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "closed", cancellationToken);
                    return;
                }

                ms.Write(buffer, 0, result.Count);
            } while (!result.EndOfMessage);

            var json = Encoding.UTF8.GetString(ms.ToArray());
            var msg = JsonSerializer.Deserialize<ChatMessage>(json, JsonOptions);
            if (msg is not null)
            {
                await onMessage(msg);
            }
        }
    }
}
