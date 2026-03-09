using Grpc.Net.Client;
using MarketData.Client.Shared.Configuration;

namespace MarketData.Client;

internal abstract class GrpcClientBase : IAsyncDisposable
{
    protected readonly GrpcChannel _channel;
    protected readonly GrpcSettings _grpcSettings;

    protected GrpcClientBase(GrpcSettings settings)
    {
        _grpcSettings = settings;
        _channel = GrpcChannel.ForAddress(settings.ServerUrl);
    }

    public virtual ValueTask DisposeAsync()
    {
        _channel.Dispose();
        return ValueTask.CompletedTask;
    }
}
