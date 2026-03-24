using MarketData.Controllers;
using MarketData.Data;
using MarketData.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MarketData.Tests.Controllers;

public class PricesControllerTests : IDisposable
{
    private readonly MarketDataContext _context;
    private readonly PricesController _controller;

    public PricesControllerTests()
    {
        var options = new DbContextOptionsBuilder<MarketDataContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
            .Options;

        _context = new MarketDataContext(options);
        _controller = new PricesController(_context);
    }

    [Fact]
    public async Task GetLatestPrice_WithExistingPrices_ReturnsLatestPrice()
    {
        var now = DateTime.UtcNow;
        _context.Prices.AddRange(
            new Price { Instrument = "AAPL", Value = 150.00m, Timestamp = now.AddMinutes(-10) },
            new Price { Instrument = "AAPL", Value = 151.50m, Timestamp = now.AddMinutes(-5) },
            new Price { Instrument = "AAPL", Value = 152.75m, Timestamp = now }
        );
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await _controller.GetLatestPrice("AAPL");

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var price = Assert.IsType<Price>(okResult.Value);
        Assert.Equal("AAPL", price.Instrument);
        Assert.Equal(152.75m, price.Value);
        Assert.Equal(now, price.Timestamp);
    }

    [Fact]
    public async Task GetLatestPrice_WithSinglePrice_ReturnsThatPrice()
    {
        var timestamp = DateTime.UtcNow;
        _context.Prices.Add(
            new Price { Instrument = "MSFT", Value = 380.25m, Timestamp = timestamp }
        );
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await _controller.GetLatestPrice("MSFT");

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var price = Assert.IsType<Price>(okResult.Value);
        Assert.Equal("MSFT", price.Instrument);
        Assert.Equal(380.25m, price.Value);
    }

    [Fact]
    public async Task GetLatestPrice_WithNonExistentInstrument_ReturnsNotFound()
    {
        var result = await _controller.GetLatestPrice("NONEXISTENT");

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal("No price data found for instrument 'NONEXISTENT'", notFoundResult.Value);
    }

    [Fact]
    public async Task GetLatestPrice_WithMultipleInstruments_ReturnsCorrectInstrumentPrice()
    {
        var now = DateTime.UtcNow;
        _context.Prices.AddRange(
            new Price { Instrument = "AAPL", Value = 150.00m, Timestamp = now },
            new Price { Instrument = "GOOGL", Value = 2800.00m, Timestamp = now },
            new Price { Instrument = "TSLA", Value = 245.50m, Timestamp = now }
        );
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await _controller.GetLatestPrice("GOOGL");

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var price = Assert.IsType<Price>(okResult.Value);
        Assert.Equal("GOOGL", price.Instrument);
        Assert.Equal(2800.00m, price.Value);
    }

    [Fact]
    public async Task GetLatestPrice_WithOutOfOrderTimestamps_ReturnsLatestByTimestamp()
    {
        var baseTime = DateTime.UtcNow;
        _context.Prices.AddRange(
            new Price { Instrument = "NVDA", Value = 500.00m, Timestamp = baseTime.AddMinutes(-20) },
            new Price { Instrument = "NVDA", Value = 510.00m, Timestamp = baseTime.AddMinutes(-5) },
            new Price { Instrument = "NVDA", Value = 505.00m, Timestamp = baseTime.AddMinutes(-10) }
        );
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await _controller.GetLatestPrice("NVDA");

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var price = Assert.IsType<Price>(okResult.Value);
        Assert.Equal(510.00m, price.Value);
        Assert.Equal(baseTime.AddMinutes(-5), price.Timestamp);
    }

    [Fact]
    public async Task GetLatestPrice_WithNoPrices_ReturnsNotFound()
    {
        var result = await _controller.GetLatestPrice("EMPTY");

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
