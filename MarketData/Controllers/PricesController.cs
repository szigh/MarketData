using MarketData.Data;
using MarketData.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MarketData.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PricesController : ControllerBase
{
    private readonly MarketDataContext _context;

    public PricesController(MarketDataContext context)
    {
        _context = context;
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

        return Ok(latestPrice);
    }
}
