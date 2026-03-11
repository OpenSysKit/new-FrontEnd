using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace OpenSysKit.UI.Services;

public class RpcException(string message) : Exception(message) { }

public class RpcClient : IDisposable
{
    private const string PipeName = "OpenSysKit";
    private const int ConnectTimeoutMs = 5000;
    private const int MaxReconnectAttempts = 3;

    private static readonly Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    private NamedPipeClientStream? _pipe;
    private StreamReader? _reader;
    private StreamWriter? _writer;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private int _idCounter;

    public bool IsConnected => _pipe?.IsConnected == true;

    public async Task ConnectAsync(int timeoutMs = ConnectTimeoutMs)
    {
        DisposePipe();

        _pipe = new NamedPipeClientStream(".", PipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
        await _pipe.ConnectAsync(timeoutMs);
        _pipe.ReadMode = PipeTransmissionMode.Byte;
        _reader = new StreamReader(_pipe, Utf8NoBom, detectEncodingFromByteOrderMarks: false, bufferSize: 4096, leaveOpen: true);
        _writer = new StreamWriter(_pipe, Utf8NoBom, bufferSize: 4096, leaveOpen: true) { AutoFlush = false };
    }

    public async Task<JsonObject?> CallAsync(string method, object? param = null, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            return await CallInternalAsync(method, param, ct);
        }
        catch (Exception ex) when (IsPipeBroken(ex))
        {
            for (int attempt = 1; attempt <= MaxReconnectAttempts; attempt++)
            {
                try
                {
                    await ConnectAsync();
                    return await CallInternalAsync(method, param, ct);
                }
                catch (Exception retryEx) when (IsPipeBroken(retryEx) && attempt < MaxReconnectAttempts)
                {
                    await Task.Delay(500 * attempt, ct);
                }
            }

            throw new RpcException($"管道连接断开且重连失败: {ex.Message}");
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<JsonObject?> CallInternalAsync(string method, object? param, CancellationToken ct)
    {
        if (_writer == null || _reader == null || _pipe?.IsConnected != true)
        {
            throw new IOException("Pipe not connected");
        }

        int id = Interlocked.Increment(ref _idCounter);

        var paramsNode = JsonSerializer.SerializeToNode(param ?? new { });
        var req = new JsonObject
        {
            ["id"] = id,
            ["method"] = method,
            ["params"] = new JsonArray(paramsNode?.DeepClone() ?? new JsonObject())
        };

        var json = req.ToJsonString();
        await _writer.WriteLineAsync(json.AsMemory(), ct);
        await _writer.FlushAsync(ct);

        string? line = await _reader.ReadLineAsync(ct);
        if (string.IsNullOrWhiteSpace(line))
        {
            throw new IOException("Pipe returned empty response (server may have closed connection)");
        }

        var resp = JsonNode.Parse(line)?.AsObject()
            ?? throw new RpcException("Invalid JSON response");

        if (resp["error"] is JsonNode err && err.GetValueKind() == JsonValueKind.String)
        {
            throw new RpcException(err.GetValue<string>());
        }

        return resp["result"]?.AsObject();
    }

    private static bool IsPipeBroken(Exception ex) =>
        ex is IOException or ObjectDisposedException or InvalidOperationException;

    private void DisposePipe()
    {
        try { _writer?.Dispose(); } catch { }
        try { _reader?.Dispose(); } catch { }
        try { _pipe?.Dispose(); } catch { }
        _writer = null;
        _reader = null;
        _pipe = null;
    }

    public void Dispose()
    {
        DisposePipe();
        _lock.Dispose();
    }
}
