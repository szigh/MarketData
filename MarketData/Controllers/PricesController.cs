using MarketData.Data;
using MarketData.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static MarketData.DTO.PriceDTO;

namespace MarketData.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PricesController : ControllerBase
{
    private readonly MarketDataContext _context;
    private readonly ILogger<PricesController> _logger;

    public PricesController(MarketDataContext context, ILogger<PricesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet("{instrument}")]
    public async Task<ActionResult<Price>> GetLatestPrice(string instrument)
    {
        var latestPrice = await _context.Prices
            .Where(p => p.Instrument == instrument)
            .OrderByDescending(p => p.Timestamp)
            .FirstOrDefaultAsync();

        if (latestPrice == null)
        {
            return NotFound($"No price data found for instrument '{instrument}'");
        }

        var dto = new PriceDto(
            latestPrice.Instrument,
            latestPrice.Value,
            latestPrice.Timestamp
        );

        return Ok(dto);
    }

    [HttpGet("{instrument}/history")]
    public async Task<ActionResult<HistoricalPricesResponseDto>> GetHistoricalPrices(
        string instrument,
        [FromQuery] DateTime start,
        [FromQuery] DateTime end,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(instrument))
        {
            return BadRequest("Instrument must be provided");
        }

        if (start == default || end == default)
        {
            return BadRequest("Both 'start' and 'end' query parameters are required");
        }

        if (start >= end)
        {
            return BadRequest("'start' must be earlier than 'end'");
        }

        _logger.LogInformation(
            "REST: GetHistoricalPrices request for '{Instrument}' from {Start} to {End}",
            instrument, start, end);

        var prices = await _context.Prices
            .Where(p => p.Instrument == instrument && p.Timestamp >= start && p.Timestamp <= end)
            .OrderBy(p => p.Timestamp)
            .Select(p => new HistoricalPriceDto(
                p.Instrument,
                p.Value,
                p.Timestamp))
            .ToListAsync(ct);

        if (prices.Count == 0)
        {
            return NotFound($"No historical price data found for instrument '{instrument}' in the requested range");
        }

        return Ok(new HistoricalPricesResponseDto(
            instrument,
            start,
            end,
            prices));
    }
}
