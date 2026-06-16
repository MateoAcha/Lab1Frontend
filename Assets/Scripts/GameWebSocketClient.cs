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
    private const int ReceiveBufferSize = 256 * 1024;
    private const int SendTimeoutMilliseconds = 3000;

    private ClientWebSocket          _ws;
    private CancellationTokenSource  _cts;
    private readonly ConcurrentQueue<string> _inbox = new ConcurrentQueue<string>();
    private volatile bool _faulted;
    private string _lastError = "";

    public bool IsConnected => _ws != null && _ws.State == WebSocketState.Open && !_faulted;
    public string LastError => _lastError;

    public async Task ConnectAsync(string url)
    {
        DisposeSocket();
        _ws  = new ClientWebSocket();
        _ws.Options.KeepAliveInterval = TimeSpan.FromSeconds(10);
        _cts = new CancellationTokenSource();
        _faulted = false;
        _lastError = "";
        await _ws.ConnectAsync(new Uri(url), _cts.Token);
        _ = ReceiveLoopAsync();
    }

    public async Task SendAsync(string message)
    {
        if (!IsConnected) return;
        CancellationTokenSource sendCts = null;
        try
        {
            byte[] bytes = Encoding.UTF8.GetBytes(message);
            sendCts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token);
            sendCts.CancelAfter(SendTimeoutMilliseconds);
            await _ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, sendCts.Token);
        }
        catch (Exception ex)
        {
            MarkFaulted(ex);
        }
        finally
        {
            if (sendCts != null)
                sendCts.Dispose();
        }
    }

    public bool TryReceive(out string message) => _inbox.TryDequeue(out message);

    private async Task ReceiveLoopAsync()
    {
        byte[] buffer = new byte[ReceiveBufferSize];
        var sb = new StringBuilder(ReceiveBufferSize);
        try
        {
            while (_ws.State == WebSocketState.Open)
            {
                sb.Clear();
                WebSocketReceiveResult result;
                do
                {
                    result = await _ws.ReceiveAsync(new ArraySegment<byte>(buffer), _cts.Token);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        MarkFaulted(null);
                        return;
                    }
                    sb.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                }
                while (!result.EndOfMessage);

                _inbox.Enqueue(sb.ToString());
            }
        }
        catch (Exception ex)
        {
            MarkFaulted(ex);
        }
    }

    public void Dispose()
    {
        DisposeSocket();
    }

    private void MarkFaulted(Exception ex)
    {
        _faulted = true;
        if (ex != null)
            _lastError = ex.Message;

        try { _ws?.Abort(); } catch { }
    }

    private void DisposeSocket()
    {
        try { _cts?.Cancel(); } catch { }
        try { _ws?.Dispose(); } catch { }
        _ws = null;
        _cts = null;
        _faulted = true;
    }
}
