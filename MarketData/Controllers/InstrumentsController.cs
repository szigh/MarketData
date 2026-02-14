using MarketData.Data;
using MarketData.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MarketData.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InstrumentsController : ControllerBase
{
    private readonly MarketDataContext _context;
    private readonly ILogger<InstrumentsController> _logger;

    public InstrumentsController(MarketDataContext context, ILogger<InstrumentsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Instrument>>> GetInstruments()
    {
        return Ok(await _context.Instruments.ToListAsync());
    }

    [HttpPost]
    public async Task<ActionResult<Instrument>> CreateInstrument(CreateInstrumentRequest request)
    {
        var existingInstrument = await _context.Instruments
            .FirstOrDefaultAsync(i => i.Name == request.Name);

        if (existingInstrument != null)
        {
            return Conflict($"Instrument '{request.Name}' already exists");
        }

        var instrument = new Instrument
        {
            Name = request.Name,
            TickIntervalMillieconds = request.TickIntervalMilliseconds
        };

        _context.Instruments.Add(instrument);

        var initialPrice = new Price
        {
            Instrument = request.Name,
            Value = request.InitialPrice,
            Timestamp = DateTime.UtcNow
        };

        _context.Prices.Add(initialPrice);

        await _context.SaveChangesAsync();

        _logger.LogInformation("Created instrument '{Name}' with initial price {Price}", 
            request.Name, request.InitialPrice);

        return CreatedAtAction(nameof(GetInstrument), new { name = instrument.Name }, instrument);
    }

    [HttpGet("{name}")]
    public async Task<ActionResult<Instrument>> GetInstrument(string name)
    {
        var instrument = await _context.Instruments
            .FirstOrDefaultAsync(i => i.Name == name);

        if (instrument == null)
        {
            return NotFound($"Instrument '{name}' not found");
        }

        return Ok(instrument);
    }

    [HttpPut("{name}/frequency")]
    public async Task<ActionResult<Instrument>> UpdateInstrumentFrequency(string name, UpdateFrequencyRequest request)
    {
        var instrument = await _context.Instruments
            .FirstOrDefaultAsync(i => i.Name == name);

        if (instrument == null)
        {
            return NotFound($"Instrument '{name}' not found");
        }

        instrument.TickIntervalMillieconds = request.TickIntervalMilliseconds;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated instrument '{Name}' tick interval to {Interval} seconds", 
            name, request.TickIntervalMilliseconds);

        return Ok(instrument);
    }

    [HttpDelete("{name}")]
    public async Task<ActionResult> DeleteInstrument(string name)
    {
        var instrument = await _context.Instruments
            .FirstOrDefaultAsync(i => i.Name == name);

        if (instrument == null)
        {
            return NotFound($"Instrument '{name}' not found");
        }

        _context.Instruments.Remove(instrument);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted instrument '{Name}' - price generation stopped", name);

        return NoContent();
    }
}

public record CreateInstrumentRequest(
    string Name,
    decimal InitialPrice,
    int TickIntervalMilliseconds = 1
);

public record UpdateFrequencyRequest(
    int TickIntervalMilliseconds
);
