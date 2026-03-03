using MarketData.Data;
using MarketData.Models;
using MarketData.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MarketData.Tests.Integration;

/// <summary>
/// Integration tests for MarketDataGeneratorService background service.
/// These tests use a real service host and test actual time-based behavior.
/// 
/// To expand: Add tests for hot reload, multiple instruments, throttling, etc.
/// </summary>
public class MarketDataGeneratorServiceIntegrationTests : IAsyncDisposable
{
    private readonly IHost _host;
    private readonly MarketDataContext _context;

    public MarketDataGeneratorServiceIntegrationTests()
    {
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                var dbName = $"IntegrationTestDb_{Guid.NewGuid()}";
                services.AddDbContext<MarketDataContext>(options =>
                    options.UseInMemoryDatabase(dbName));

                // dependencies for MarketDataGeneratorService
                services.AddSingleton<IPriceSimulatorFactory, PriceSimulatorFactory>();
                services.AddSingleton<IInstrumentModelManager, InstrumentModelManager>();

                // Configure with FAST intervals for testing (not production values!)
                services.Configure<MarketDataGeneratorOptions>(options =>
                {
                    //TODO tests for throttling and backpressure
                    options.CheckIntervalMilliseconds = 50;
                    options.DatabasePersistenceMilliseconds = 50;
                    options.GrpcPublishMilliseconds = 50;
                });

                // Register the background service (system under test)
                services.AddHostedService<MarketDataGeneratorService>();

                services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Warning));
            })
            .Build();

        _context = _host.Services.GetRequiredService<MarketDataContext>();
    }

    [Fact]
    public async Task Service_StartsAndGeneratesPrices()
    {
        var instrument = new Instrument
        {
            Name = "TEST",
            ModelType = "Flat",
            TickIntervalMillieconds = 100,
            FlatConfig = new FlatConfig()
        };
        _context.Instruments.Add(instrument);
        
        _context.Prices.Add(new Price
        {
            Instrument = "TEST",
            Value = 100m,
            Timestamp = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var initialPriceCount = await _context.Prices
            .Where(p => p.Instrument == "TEST")
            .CountAsync();

        await _host.StartAsync();

        // Poll until at least one new price is generated, or timeout after 5 seconds
        var finalPriceCount = initialPriceCount;
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (finalPriceCount <= initialPriceCount && DateTime.UtcNow < deadline)
        {
            await Task.Delay(50);
            finalPriceCount = await _context.Prices
                .Where(p => p.Instrument == "TEST")
                .CountAsync();
        }

        await _host.StopAsync(TimeSpan.FromSeconds(2));

        Assert.True(finalPriceCount > initialPriceCount, 
            $"Expected more than {initialPriceCount} prices, got {finalPriceCount}");
    }

    [Fact]
    public async Task Service_RespectsTickInterval()
    {
        var fastInstrument = new Instrument
        {
            Name = "FAST",
            ModelType = "Flat",
            TickIntervalMillieconds = 50,
            FlatConfig = new FlatConfig()
        };

        var slowInstrument = new Instrument
        {
            Name = "SLOW",
            ModelType = "Flat",
            TickIntervalMillieconds = 200,
            FlatConfig = new FlatConfig()
        };

        _context.Instruments.AddRange(fastInstrument, slowInstrument);
        _context.Prices.AddRange(
            new Price { Instrument = "FAST", Value = 100m, Timestamp = DateTime.UtcNow },
            new Price { Instrument = "SLOW", Value = 200m, Timestamp = DateTime.UtcNow }
        );
        await _context.SaveChangesAsync();

        await _host.StartAsync();

        // Poll until fast instrument has generated noticeably more prices than slow,
        // or timeout after 10 seconds (allows enough ticks at both intervals)
        var fastCount = 1;
        var slowCount = 1;
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (DateTime.UtcNow < deadline)
        {
            await Task.Delay(100);
            fastCount = await _context.Prices.CountAsync(p => p.Instrument == "FAST");
            slowCount = await _context.Prices.CountAsync(p => p.Instrument == "SLOW");
            // Wait until both have generated prices and fast clearly exceeds slow
            if (fastCount >= 3 && fastCount > slowCount)
                break;
        }

        await _host.StopAsync(TimeSpan.FromSeconds(2));

        Assert.True(fastCount > slowCount,
            $"Fast instrument ({fastCount} prices) should have more than slow instrument ({slowCount} prices)");
    }

    [Fact]
    public async Task Service_StopsCleanly()
    {
        _context.Instruments.Add(new Instrument
        {
            Name = "TEST",
            ModelType = "Flat",
            TickIntervalMillieconds = 100,
            FlatConfig = new FlatConfig()
        });
        _context.Prices.Add(new Price
        {
            Instrument = "TEST",
            Value = 100m,
            Timestamp = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        await _host.StartAsync();
        await Task.Delay(100);
        var stopTask = _host.StopAsync(TimeSpan.FromSeconds(3));

        var completed = await Task.WhenAny(stopTask, Task.Delay(5000)) == stopTask;

        Assert.True(completed, "Service should stop cleanly within timeout");
    }

    public async ValueTask DisposeAsync()
    {
        await _host.StopAsync(TimeSpan.FromSeconds(1));
        
        _host.Dispose();
        await _context.DisposeAsync();
    }
}
