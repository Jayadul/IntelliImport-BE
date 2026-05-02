using System.Threading.Channels;
using IntelliImport.Application.Abstractions;

namespace IntelliImport.Infrastructure.Services;

public sealed class JobQueue : IJobQueue
{
    private readonly Channel<Guid> _channel =
        Channel.CreateUnbounded<Guid>(new UnboundedChannelOptions { SingleReader = true });

    public void Enqueue(Guid jobId) =>
        _channel.Writer.TryWrite(jobId);

    public Task<Guid> DequeueAsync(CancellationToken ct) =>
        _channel.Reader.ReadAsync(ct).AsTask();
}