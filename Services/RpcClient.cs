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
    private NamedPipeClientStream? _pipe;
    private StreamReader? _reader;
    private StreamWriter? _writer;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private int _idCounter = 0;

    public bool IsConnected => _pipe?.IsConnected == true;

    public async Task ConnectAsync(int timeoutMs = 3000)
    {
        _pipe = new NamedPipeClientStream(".", PipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
        await _pipe.ConnectAsync(timeoutMs);
        _reader = new StreamReader(_pipe, Encoding.UTF8, leaveOpen: true);
        _writer = new StreamWriter(_pipe, Encoding.UTF8, leaveOpen: true) { AutoFlush = true };
    }

    public async Task<JsonObject?> CallAsync(string method, object? param = null, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            int id = Interlocked.Increment(ref _idCounter);
            var req = new JsonObject
            {
                ["id"] = id,
                ["method"] = method,
                ["params"] = JsonSerializer.SerializeToNode(param ?? new { })
                    is JsonNode n ? new JsonArray(n) : new JsonArray(new JsonObject())
            };

            await _writer!.WriteLineAsync(req.ToJsonString());
            await _writer.FlushAsync(ct);

            string? line = await _reader!.ReadLineAsync(ct);
            if (string.IsNullOrWhiteSpace(line))
                throw new RpcException("Empty response from server");

            var resp = JsonNode.Parse(line)?.AsObject()
                ?? throw new RpcException("Invalid JSON response");

            if (resp["error"] is JsonNode err && err.GetValueKind() == System.Text.Json.JsonValueKind.String)
                throw new RpcException(err.GetValue<string>());

            return resp["result"]?.AsObject();
        }
        finally
        {
            _lock.Release();
        }
    }

    public void Dispose()
    {
        _writer?.Dispose();
        _reader?.Dispose();
        _pipe?.Dispose();
    }
}
