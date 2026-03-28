using MarketData.Data;
using MarketData.Models;
using MarketData.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static MarketData.DTO.InstrumentsDTO;
using static MarketData.DTO.ModelConfigurationsDTO;

namespace MarketData.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InstrumentsController : ControllerBase
{
    private readonly IInstrumentModelManager _modelManager;
    private readonly MarketDataContext _context;
    private readonly ILogger<InstrumentsController> _logger;

    public InstrumentsController(IInstrumentModelManager modelManager, MarketDataContext context, ILogger<InstrumentsController> logger)
    {
        _modelManager = modelManager;
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<InstrumentConfigurationsResponseDto>>> GetInstruments(CancellationToken ct)
    {
        var instruments = await _modelManager.LoadAndInitializeAllInstrumentsAsync(ct);

        var response = instruments.Values
            .Select(MarketData.DTO.InstrumentsDTO.MapToDto)
            .ToList();

        return Ok(response);
    }

    [HttpPost]
    public async Task<ActionResult<Instrument>> CreateInstrument([FromBody] CreateInstrumentRequestDto request, CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.InstrumentName))
            {
                return BadRequest("InstrumentName must be provided");
            }

            if (request.TickIntervalMs <= 0)
            {
                return BadRequest("TickIntervalMs must be greater than zero");
            }

            if (request.InitialPriceTimestamp == default)
            {
                return BadRequest("InitialPriceTimestamp must be a valid non-default timestamp");
            }

            var (instrument, created) = await _modelManager.GetOrCreateInstrumentAsync(
                request.InstrumentName,
                request.TickIntervalMs,
                request.InitialPriceValue,
                request.InitialPriceTimestamp,
                request.ModelType,
                ct);

            var dto = new CreateInstrumentResponseDto(
                Message: created
                    ? $"Instrument '{request.InstrumentName}' added successfully"
                    : $"Instrument '{request.InstrumentName}' already exists",
                Added: created,
                InstrumentName: instrument.Name,
                ActiveModel: instrument.ModelType ?? "None",
                TickIntervalMs: instrument.TickIntervalMillieconds
            );

            if (created)
            {
                return CreatedAtAction(
                    nameof(GetInstrument),
                    new { name = instrument.Name },
                    dto);
            }

            return Ok(dto);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("{name}")]
    public async Task<ActionResult<InstrumentConfigurationsResponseDto>> GetInstrument(
    string name,
    CancellationToken ct)
    {
        var instrument = await _modelManager.GetInstrumentWithConfigurationsAsync(name, ct);

        if (instrument == null)
        {
            return NotFound($"Instrument '{name}' not found");
        }

        var dto = MarketData.DTO.InstrumentsDTO.MapToDto(instrument);

        return Ok(dto);
    }

    [HttpPut("{name}/frequency")]
    public async Task<ActionResult<UpdateInstrumentResponseDto>> UpdateInstrumentFrequency(
    string name,
    [FromBody] UpdateTickIntervalRequestDto request,
    CancellationToken ct)
    {
        try
        {
            var updatedValue = await _modelManager.UpdateTickIntervalAsync(
                name,
                request.TickIntervalMs,
                ct);

            var success = updatedValue == request.TickIntervalMs;

            var dto = new UpdateInstrumentResponseDto(
                Message: success
                    ? "Tick interval updated successfully"
                    : $"Tick interval update failed. Current value is {updatedValue} ms",
                Success: success
            );

            return Ok(dto);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpDelete("{name}")]
    public async Task<ActionResult<RemoveInstrumentResponseDto>> DeleteInstrument(
    string name,
    CancellationToken ct)
    {
        try
        {
            var removed = await _modelManager.TryRemoveInstrumentAsync(name, ct);

            var dto = new RemoveInstrumentResponseDto(
                Message: removed
                    ? $"Instrument '{name}' removed successfully"
                    : $"Instrument '{name}' not found",
                Removed: removed
            );

            if (!removed)
            {
                return NotFound(dto);
            }

            return Ok(dto);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
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
