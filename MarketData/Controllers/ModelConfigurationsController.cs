using MarketData.Grpc;
using MarketData.PriceSimulator;
using MarketData.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using static MarketData.DTO.ModelConfigurationsDTO;

namespace MarketData.Controllers;

/// <summary>
/// API controller for managing instrument model configurations.
/// This is a thin controller that delegates all business logic to InstrumentModelManager.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ModelConfigurationsController : ControllerBase
{
    private readonly IInstrumentModelManager _modelManager;

    public ModelConfigurationsController(IInstrumentModelManager modelManager)
    {
        _modelManager = modelManager;
    }

    /// <summary>
    /// Get the current active model and all available configurations for an instrument
    /// </summary>
    [HttpGet("{instrumentName}")]
    public async Task<ActionResult<object>> GetConfigurations(string instrumentName, CancellationToken ct)
    {
        var instrument = await _modelManager.GetInstrumentWithConfigurationsAsync(instrumentName, ct);

        if (instrument == null)
        {
            return NotFound($"Instrument '{instrumentName}' not found");
        }


        RandomMultiplicativeConfigDto? randomMultiplicative = null;
        if (instrument.RandomMultiplicativeConfig != null)
        {
            randomMultiplicative = new RandomMultiplicativeConfigDto(
                instrument.RandomMultiplicativeConfig.StandardDeviation,
                instrument.RandomMultiplicativeConfig.Mean
            );
        }

        MeanRevertingConfigDto? meanReverting = null;
        if (instrument.MeanRevertingConfig != null)
        {
            meanReverting = new MeanRevertingConfigDto(
                instrument.MeanRevertingConfig.Mean,
                instrument.MeanRevertingConfig.Kappa,
                instrument.MeanRevertingConfig.Sigma,
                instrument.MeanRevertingConfig.Dt
            );
        }

        RandomAdditiveWalkConfigDto? randomAdditiveWalk = null;
        if (instrument.RandomAdditiveWalkConfig != null)
        {
            var walkSteps = JsonSerializer.Deserialize<List<RandomWalkStep>>(
                instrument.RandomAdditiveWalkConfig.WalkStepsJson
            ) ?? new List<RandomWalkStep>();

            randomAdditiveWalk = new RandomAdditiveWalkConfigDto(
                walkSteps.Select(s => new WalkStepDto(
                    s.Probability,
                    s.Value
                )).ToList()
            );
        }

        var dto = new InstrumentConfigurationsResponseDto(
            InstrumentName: instrument.Name,
            ActiveModel: instrument.ModelType ?? "None",
            RandomMultiplicative: randomMultiplicative,
            MeanReverting: meanReverting,
            FlatConfigured: instrument.FlatConfig != null,
            RandomAdditiveWalk: randomAdditiveWalk,
            TickIntervalMs: instrument.TickIntervalMillieconds
        );

        return Ok(dto);
    }

    /// <summary>
    /// Switch the active model for an instrument
    /// </summary>
    [HttpPut("{instrumentName}/model")]
    public async Task<ActionResult> SwitchModel(
        string instrumentName,
        [FromBody] SwitchModelRequestDto request,
        CancellationToken ct)
    {
        try
        {
            var previousModel = await _modelManager.SwitchModelAsync(instrumentName, request.ModelType, ct);
            var dto = new SwitchModelResponseDto(
                Message: "Model switched successfully. Changes are applied immediately.",
                PreviousModel: previousModel ?? "None",
                NewModel: request.ModelType
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

    /// <summary>
    /// Update RandomMultiplicative configuration
    /// </summary>
    [HttpPut("{instrumentName}/config/random-multiplicative")]
    public async Task<ActionResult> UpdateRandomMultiplicativeConfig(
        string instrumentName,
        [FromBody] UpdateRandomMultiplicativeRequestDto request,
        CancellationToken ct)
    {
        try
        {
            var config = await _modelManager.UpdateRandomMultiplicativeConfigAsync(
                instrumentName,
                request.StandardDeviation,
                request.Mean,
                ct);

            var dto = new UpdateConfigResponseDto(
                Message: "RandomMultiplicative configuration updated successfully",
                Success: true
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

    /// <summary>
    /// Update MeanReverting configuration
    /// </summary>
    [HttpPut("{instrumentName}/config/mean-reverting")]
    public async Task<ActionResult> UpdateMeanRevertingConfig(
        string instrumentName,
        [FromBody] UpdateMeanRevertingRequestDto request,
        CancellationToken ct)
    {
        try
        {
            var config = await _modelManager.UpdateMeanRevertingConfigAsync(
                instrumentName,
                request.Mean,
                request.Kappa,
                request.Sigma,
                request.Dt,
                ct);

            var dto = new UpdateConfigResponseDto(
                Message: "MeanReverting configuration updated successfully",
                Success: true
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

    /// <summary>
    /// Update RandomAdditiveWalk configuration
    /// </summary>
    [HttpPut("{instrumentName}/config/random-additive-walk")]
    public async Task<ActionResult> UpdateRandomAdditiveWalkConfig(
        string instrumentName,
        [FromBody] UpdateRandomAdditiveWalkRestRequest request,
        CancellationToken ct)
    {
        try
        {
            if (request.WalkSteps == null || request.WalkSteps.Count == 0)
            {
                return BadRequest("Walk steps cannot be empty");
            }

            // Validate probabilities sum to ~1.0
            var totalProbability = request.WalkSteps.Sum(s => s.Probability);
            if (Math.Abs(totalProbability - 1.0) > 0.0001)
            {
                return BadRequest($"Walk step probabilities must sum to 1.0 (got {totalProbability})");
            }

            if (request.WalkSteps.Any(s => s.Probability < 0 || s.Probability > 1))
            {
                return BadRequest("Walk step probabilities must all be between 0 and 1");
            }

            // Convert to JSON (gleich wie im gRPC-Service)
            var walkStepsForJson = request.WalkSteps.Select(s => new
            {
                Probability = s.Probability,
                Value = s.StepValue
            }).ToList();

            var walkStepsJson = System.Text.Json.JsonSerializer.Serialize(walkStepsForJson);

            await _modelManager.UpdateRandomAdditiveWalkConfigAsync(
                instrumentName,
                walkStepsJson,
                ct);

            var dto = new UpdateConfigResponseDto(
                Message: "RandomAdditiveWalk configuration updated successfully",
                Success: true
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
}

public record SwitchModelRequest(string ModelType);

public record UpdateRandomMultiplicativeRequest(
    double StandardDeviation,
    double Mean);

public record UpdateMeanRevertingRequest(
    double Mean,
    double Kappa,
    double Sigma,
    double Dt);
