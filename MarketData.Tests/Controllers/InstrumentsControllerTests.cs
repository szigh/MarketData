using MarketData.Controllers;
using MarketData.Data;
using MarketData.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace MarketData.Tests.Controllers;

public class InstrumentsControllerTests : IDisposable
{
    private readonly MarketDataContext _context;
    private readonly InstrumentsController _controller;

    public InstrumentsControllerTests()
    {
        var options = new DbContextOptionsBuilder<MarketDataContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
            .Options;

        _context = new MarketDataContext(options);
        _controller = new InstrumentsController(_context, 
            NullLogger<InstrumentsController>.Instance);
    }

    [Fact]
    public async Task CreateInstrument_WithValidData_ReturnsCreatedResult()
    {
        var request = new CreateInstrumentRequest("FTSE", 10050.50m, 1000);

        var result = await _controller.CreateInstrument(request, CancellationToken.None);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var instrument = Assert.IsType<Instrument>(createdResult.Value);

        Assert.Equal("FTSE", instrument.Name);
        Assert.Equal(1000, instrument.TickIntervalMillieconds);

        var price = await _context.Prices.FirstOrDefaultAsync(p => p.Instrument == "FTSE", TestContext.Current.CancellationToken);
        Assert.NotNull(price);
        Assert.Equal(10050.50m, price.Value);
    }

    [Fact]
    public async Task CreateInstrument_WithDuplicateName_ReturnsConflict()
    {
        var existing = new Instrument { Name = "AAPL", TickIntervalMillieconds = 500 };
        _context.Instruments.Add(existing);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var request = new CreateInstrumentRequest("AAPL", 150.00m, 1000);

        var result = await _controller.CreateInstrument(request, CancellationToken.None);

        var conflictResult = Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.Equal("Instrument 'AAPL' already exists", conflictResult.Value);
    }

    [Fact]
    public async Task GetInstruments_WithNoInstruments_ReturnsEmptyList()
    {
        var result = await _controller.GetInstruments(CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var instruments = Assert.IsType<IEnumerable<Instrument>>(okResult.Value, exactMatch: false);
        Assert.Empty(instruments);
    }

    [Fact]
    public async Task GetInstruments_WithMultipleInstruments_ReturnsAllInstruments()
    {
        _context.Instruments.AddRange(
            new Instrument { Name = "AAPL", TickIntervalMillieconds = 1000 },
            new Instrument { Name = "GOOGL", TickIntervalMillieconds = 2000 },
            new Instrument { Name = "MSFT", TickIntervalMillieconds = 1500 }
        );
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await _controller.GetInstruments(CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var instruments = Assert.IsType<IEnumerable<Instrument>>(okResult.Value, exactMatch: false);
        Assert.Equal(3, instruments.Count());
    }

    [Fact]
    public async Task GetInstrument_WithExistingName_ReturnsInstrument()
    {
        var instrument = new Instrument { Name = "TSLA", TickIntervalMillieconds = 800 };
        _context.Instruments.Add(instrument);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await _controller.GetInstrument("TSLA", CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedInstrument = Assert.IsType<Instrument>(okResult.Value);
        Assert.Equal("TSLA", returnedInstrument.Name);
        Assert.Equal(800, returnedInstrument.TickIntervalMillieconds);
    }

    [Fact]
    public async Task GetInstrument_WithNonExistentName_ReturnsNotFound()
    {
        var result = await _controller.GetInstrument("NONEXISTENT", CancellationToken.None);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal("Instrument 'NONEXISTENT' not found", notFoundResult.Value);
    }

    [Fact]
    public async Task UpdateInstrumentFrequency_WithExistingInstrument_UpdatesFrequency()
    {
        var instrument = new Instrument { Name = "NVDA", TickIntervalMillieconds = 1000 };
        _context.Instruments.Add(instrument);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var request = new UpdateFrequencyRequest(2500);

        var result = await _controller.UpdateInstrumentFrequency("NVDA", request, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var updatedInstrument = Assert.IsType<Instrument>(okResult.Value);
        Assert.Equal(2500, updatedInstrument.TickIntervalMillieconds);

        var dbInstrument = await _context.Instruments.FirstOrDefaultAsync(i => i.Name == "NVDA", TestContext.Current.CancellationToken);
        Assert.NotNull(dbInstrument);
        Assert.Equal(2500, dbInstrument.TickIntervalMillieconds);
    }

    [Fact]
    public async Task UpdateInstrumentFrequency_WithNonExistentInstrument_ReturnsNotFound()
    {
        var request = new UpdateFrequencyRequest(3000);

        var result = await _controller.UpdateInstrumentFrequency("NONEXISTENT", request, CancellationToken.None);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal("Instrument 'NONEXISTENT' not found", notFoundResult.Value);
    }

    [Fact]
    public async Task DeleteInstrument_WithExistingInstrument_DeletesInstrument()
    {
        var instrument = new Instrument { Name = "AMD", TickIntervalMillieconds = 1200 };
        _context.Instruments.Add(instrument);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await _controller.DeleteInstrument("AMD", CancellationToken.None);

        Assert.IsType<NoContentResult>(result);

        var deletedInstrument = await _context.Instruments.FirstOrDefaultAsync(i => i.Name == "AMD", TestContext.Current.CancellationToken);
        Assert.Null(deletedInstrument);
    }

    [Fact]
    public async Task DeleteInstrument_WithNonExistentInstrument_ReturnsNotFound()
    {
        var result = await _controller.DeleteInstrument("NONEXISTENT", CancellationToken.None);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal("Instrument 'NONEXISTENT' not found", notFoundResult.Value);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
