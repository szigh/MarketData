using Grpc.Net.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MarketData.Client.Shared.Services;

public interface IGrpcConnectionInitializer
{
    Task InitializeAsync(int maxRetries = 5, int initialRetryDelayMs = 100, CancellationToken ct = default);
}

public class GrpcConnectionInitializer : IGrpcConnectionInitializer
{
    private readonly ILogger _logger;
    private readonly GrpcChannel _channel;

    public GrpcConnectionInitializer(GrpcChannel channel, ILogger? logger = null)
    {
        _logger = logger ?? NullLogger.Instance;
        _channel = channel;
    }

    public async Task InitializeAsync(int maxRetries = 5, int initialRetryDelayMs = 100, 
        CancellationToken ct = default)
    {
        _logger.LogInformation("Initializing gRPC connection to {Address}", _channel.Target);

        var retryDelay = TimeSpan.FromMilliseconds(initialRetryDelayMs);
        Exception? lastException = null;

        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                await _channel.ConnectAsync(ct);
                _logger.LogInformation("gRPC connection established successfully");
                return;
            }
            catch (Exception ex)
            {
                lastException = ex;
                if (i < maxRetries - 1)
                {
                    _logger.LogWarning("gRPC connection attempt {Attempt} of {MaxRetries} failed: {Message}",
                        i + 1, maxRetries, ex.Message);

                    _logger.LogInformation("Waiting {Delay} before next retry", retryDelay);
                    await Task.Delay(retryDelay, ct);
                    retryDelay = TimeSpan.FromMilliseconds(retryDelay.TotalMilliseconds * 1.5);
                }
            }
        }

        _logger.LogError(lastException, "Failed to establish gRPC connection after {MaxRetries} attempts", maxRetries);
        throw lastException ?? new TimeoutException($"Failed to establish gRPC connection after {maxRetries} attempts");
    }
}
