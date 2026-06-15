using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

// Thin wrapper around ClientWebSocket with a thread-safe inbox queue.
// Connect/Send are async; receive runs on a background thread and enqueues
// messages so the main thread can drain them via TryReceive().
public class GameWebSocketClient : IDisposable
{
    private ClientWebSocket          _ws;
    private CancellationTokenSource  _cts;
    private readonly ConcurrentQueue<string> _inbox = new ConcurrentQueue<string>();

    public bool IsConnected => _ws != null && _ws.State == WebSocketState.Open;

    public async Task ConnectAsync(string url)
    {
        _ws  = new ClientWebSocket();
        _cts = new CancellationTokenSource();
        await _ws.ConnectAsync(new Uri(url), _cts.Token);
        _ = ReceiveLoopAsync();
    }

    public async Task SendAsync(string message)
    {
        if (!IsConnected) return;
        try
        {
            byte[] bytes = Encoding.UTF8.GetBytes(message);
            await _ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, _cts.Token);
        }
        catch { }
    }

    public bool TryReceive(out string message) => _inbox.TryDequeue(out message);

    private async Task ReceiveLoopAsync()
    {
        byte[] buffer = new byte[64 * 1024];
        var sb = new StringBuilder();
        try
        {
            while (_ws.State == WebSocketState.Open)
            {
                sb.Clear();
                WebSocketReceiveResult result;
                do
                {
                    result = await _ws.ReceiveAsync(new ArraySegment<byte>(buffer), _cts.Token);
                    if (result.MessageType == WebSocketMessageType.Close) return;
                    sb.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                }
                while (!result.EndOfMessage);

                _inbox.Enqueue(sb.ToString());
            }
        }
        catch { }
    }

    public void Dispose()
    {
        try { _cts?.Cancel(); } catch { }
        try { _cts?.Dispose(); } catch { }
        try { _ws?.Dispose(); } catch { }
        _cts = null;
        _ws  = null;
    }
}
