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
        _channel = GrpcChannel.ForAddress(settings.ServerUrl, new GrpcChannelOptions
        {
            // Ensure connection attempts happen faster
            InitialReconnectBackoff = TimeSpan.FromMilliseconds(100),
            MaxReconnectBackoff = TimeSpan.FromSeconds(1),
        });
    }

    protected async Task WaitForConnectionAsync(CancellationToken cancellationToken = default)
    {
        var maxRetries = 5;
        var retryDelay = TimeSpan.FromMilliseconds(100);

        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                await _channel.ConnectAsync(cancellationToken);
                return;
            }
            catch (Exception) when (i < maxRetries - 1)
            {
                await Task.Delay(retryDelay, cancellationToken);
                retryDelay = TimeSpan.FromMilliseconds(retryDelay.TotalMilliseconds * 1.5);
            }
        }
    }

    public virtual ValueTask DisposeAsync()
    {
        _channel.Dispose();
        return ValueTask.CompletedTask;
    }
}
