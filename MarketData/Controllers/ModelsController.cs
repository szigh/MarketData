using MarketData.Services;
using Microsoft.AspNetCore.Mvc;
using static MarketData.DTO.ModelDTO;

namespace MarketData.Controllers;

/// <summary>
/// API controller for managing instrument model configurations.
/// This is a thin controller that delegates all business logic to InstrumentModelManager.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ModelsController : ControllerBase
{
    private readonly ILogger<ModelsController> _logger;

    public ModelsController(ILogger<ModelsController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Get all supported model types
    /// </summary>
    [HttpGet]
    public ActionResult<object> GetSupportedModels()
    {
        try
        {
            _logger.LogInformation("REST: GetSupportedModels request");

            var supportedModels = InstrumentModelManager.GetSupportedModelTypes();
            var dto = new SupportedModelsResponseDto(supportedModels.ToList<string>());
            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting supported models");
            return StatusCode(500, "Internal server error");
        }
    }

}

