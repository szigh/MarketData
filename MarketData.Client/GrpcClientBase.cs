using Grpc.Net.Client;
using MarketData.Client.Grpc.Configuration;
using MarketData.Client.Grpc.Services;

namespace MarketData.Client;

internal abstract class GrpcClientBase : IAsyncDisposable
{
    protected readonly GrpcChannel _channel;
    protected readonly GrpcSettings _grpcSettings;

    protected GrpcClientBase(GrpcSettings settings)
    {
        _grpcSettings = settings;
        _channel = GrpcChannel.ForAddress(settings.ServerUrl, new GrpcChannelOptions
        {
            // Ensure connection attempts happen faster
            InitialReconnectBackoff = TimeSpan.FromMilliseconds(100),
            MaxReconnectBackoff = TimeSpan.FromSeconds(1),
        });
    }

    protected async Task WaitForConnectionAsync(CancellationToken ct = default)
    {
        var initializer = new GrpcConnectionInitializer(_channel);
        await initializer.InitializeAsync(ct: ct);
    }

    public virtual ValueTask DisposeAsync()
    {
        _channel.Dispose();
        return ValueTask.CompletedTask;
    }
}
