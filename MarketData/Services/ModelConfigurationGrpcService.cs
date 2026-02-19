using Grpc.Core;
using MarketData.Grpc;
using MarketData.Services;

namespace MarketData.Services;

/// <summary>
/// gRPC service for managing instrument model configurations.
/// </summary>
public class ModelConfigurationGrpcService : ModelConfigurationService.ModelConfigurationServiceBase
{
    private readonly IInstrumentModelManager _modelManager;
    private readonly ILogger<ModelConfigurationGrpcService> _logger;

    public ModelConfigurationGrpcService(
        IInstrumentModelManager modelManager,
        ILogger<ModelConfigurationGrpcService> logger)
    {
        _modelManager = modelManager;
        _logger = logger;
    }

    public override async Task<SupportedModelsResponse> GetSupportedModels(
        GetSupportedModelsRequest request,
        ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("gRPC: GetSupportedModels request");
            var supportedModels = InstrumentModelManager.GetSupportedModelTypes();
            return new SupportedModelsResponse
            {
                SupportedModels = { supportedModels }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting supported models");
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    /// <summary>
    /// Get the current active model and all available configurations for an instrument
    /// </summary>
    public override async Task<ConfigurationsResponse> GetConfigurations(
        GetConfigurationsRequest request,
        ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("gRPC: GetConfigurations request for '{InstrumentName}'", request.InstrumentName);

            var instrument = await _modelManager.GetInstrumentWithConfigurationsAsync(request.InstrumentName);

            if (instrument == null)
            {
                throw new RpcException(new Status(
                    StatusCode.NotFound,
                    $"Instrument '{request.InstrumentName}' not found"));
            }

            var response = new ConfigurationsResponse
            {
                InstrumentName = instrument.Name,
                ActiveModel = instrument.ModelType ?? "None"
            };

            // Add RandomMultiplicative configuration if exists
            if (instrument.RandomMultiplicativeConfig != null)
            {
                response.RandomMultiplicative = new RandomMultiplicativeConfigData
                {
                    StandardDeviation = instrument.RandomMultiplicativeConfig.StandardDeviation,
                    Mean = instrument.RandomMultiplicativeConfig.Mean
                };
            }

            // Add MeanReverting configuration if exists
            if (instrument.MeanRevertingConfig != null)
            {
                response.MeanReverting = new MeanRevertingConfigData
                {
                    Mean = instrument.MeanRevertingConfig.Mean,
                    Kappa = instrument.MeanRevertingConfig.Kappa,
                    Sigma = instrument.MeanRevertingConfig.Sigma,
                    Dt = instrument.MeanRevertingConfig.Dt
                };
            }

            // Add Flat configuration indicator
            response.FlatConfigured = instrument.FlatConfig != null;

            // Add RandomAdditiveWalk configuration if exists
            if (instrument.RandomAdditiveWalkConfig != null)
            {
                var walkStepsData = new RandomAdditiveWalkConfigData();

                // Deserialize JSON to get walk steps
                var walkSteps = System.Text.Json.JsonSerializer.Deserialize<List<MarketData.PriceSimulator.RandomWalkStep>>(
                    instrument.RandomAdditiveWalkConfig.WalkStepsJson);

                if (walkSteps != null)
                {
                    foreach (var step in walkSteps)
                    {
                        walkStepsData.WalkSteps.Add(new WalkStep
                        {
                            Probability = step.Probability,
                            StepValue = step.Value
                        });
                    }
                }

                response.RandomAdditiveWalk = walkStepsData;
            }

            response.TickIntervalMs = instrument.TickIntervalMillieconds;

            _logger.LogDebug("Returning configurations for '{InstrumentName}' with active model '{ModelType}'",
                instrument.Name, instrument.ModelType);

            return response;
        }
        catch (RpcException)
        {
            throw; // Re-throw RpcExceptions as-is
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting configurations for '{InstrumentName}'", request.InstrumentName);
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    public override async Task<UpdateConfigResponse> UpdateTickInterval(UpdateTickIntervalRequest request, ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("gRPC: UpdateTickInterval request for '{InstrumentName}' to '{TickIntervalMs}' ms",
                request.InstrumentName, request.TickIntervalMs);
            if (request.TickIntervalMs <= 0)
            {
                throw new ArgumentException("Tick interval must be a positive integer");
            }

            var updatedValue = await _modelManager.UpdateTickIntervalAsync(request.InstrumentName, request.TickIntervalMs);

            if (updatedValue == request.TickIntervalMs)
            {
                return new UpdateConfigResponse
                {
                    Message = "Tick interval updated successfully",
                    Success = true
                };
            }
            else
            {
                return new UpdateConfigResponse
                {
                    Message = $"Tick interval update failed. Current value is {updatedValue} ms",
                    Success = false
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating tick interval for '{InstrumentName}'", request.InstrumentName);
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    /// <summary>
    /// Switch the active model for an instrument
    /// </summary>
    public override async Task<SwitchModelResponse> SwitchModel(
        SwitchModelRequest request,
        ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("gRPC: SwitchModel request for '{InstrumentName}' to '{ModelType}'",
                request.InstrumentName, request.ModelType);

            var previousModel = await _modelManager.SwitchModelAsync(
                request.InstrumentName,
                request.ModelType);

            return new SwitchModelResponse
            {
                Message = "Model switched successfully. Changes are applied immediately.",
                PreviousModel = previousModel ?? "None",
                NewModel = request.ModelType
            };
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for SwitchModel");
            throw new RpcException(new Status(StatusCode.InvalidArgument, ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Instrument not found for SwitchModel");
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error switching model for '{InstrumentName}'", request.InstrumentName);
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    /// <summary>
    /// Update RandomMultiplicative configuration
    /// </summary>
    public override async Task<UpdateConfigResponse> UpdateRandomMultiplicativeConfig(
        UpdateRandomMultiplicativeRequest request,
        ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("gRPC: UpdateRandomMultiplicativeConfig for '{InstrumentName}'",
                request.InstrumentName);

            await _modelManager.UpdateRandomMultiplicativeConfigAsync(
                request.InstrumentName,
                request.StandardDeviation,
                request.Mean);

            return new UpdateConfigResponse
            {
                Message = "RandomMultiplicative configuration updated successfully",
                Success = true
            };
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for UpdateRandomMultiplicativeConfig");
            throw new RpcException(new Status(StatusCode.InvalidArgument, ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Instrument not found for UpdateRandomMultiplicativeConfig");
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating RandomMultiplicative config for '{InstrumentName}'",
                request.InstrumentName);
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    /// <summary>
    /// Update MeanReverting configuration
    /// </summary>
    public override async Task<UpdateConfigResponse> UpdateMeanRevertingConfig(
        UpdateMeanRevertingRequest request,
        ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("gRPC: UpdateMeanRevertingConfig for '{InstrumentName}'",
                request.InstrumentName);

            await _modelManager.UpdateMeanRevertingConfigAsync(
                request.InstrumentName,
                request.Mean,
                request.Kappa,
                request.Sigma,
                request.Dt);

            return new UpdateConfigResponse
            {
                Message = "MeanReverting configuration updated successfully",
                Success = true
            };
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for UpdateMeanRevertingConfig");
            throw new RpcException(new Status(StatusCode.InvalidArgument, ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Instrument not found for UpdateMeanRevertingConfig");
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating MeanReverting config for '{InstrumentName}'",
                request.InstrumentName);
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }

    /// <summary>
    /// Update RandomAdditiveWalk configuration
    /// </summary>
    public override async Task<UpdateConfigResponse> UpdateRandomAdditiveWalkConfig(
        UpdateRandomAdditiveWalkRequest request,
        ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("gRPC: UpdateRandomAdditiveWalkConfig for '{InstrumentName}'",
                request.InstrumentName);

            // Validate walk steps
            if (request.WalkSteps == null || request.WalkSteps.Count == 0)
            {
                throw new ArgumentException("Walk steps cannot be empty");
            }

            // Validate probabilities sum to approximately 1.0
            var totalProbability = request.WalkSteps.Sum(s => s.Probability);
            if (Math.Abs(totalProbability - 1.0) > 0.0001)
            {
                throw new ArgumentException(
                    $"Walk step probabilities must sum to 1.0 (got {totalProbability})");
            }
            if (request.WalkSteps.Any(s => s.Probability < 0 || s.Probability > 1))
            {
                throw new ArgumentException("Walk step probabilities must all be between zero and 1");
            }

            // Convert proto WalkSteps to internal format for JSON serialization
            var walkStepsForJson = request.WalkSteps.Select(s => new
            {
                Probability = s.Probability,
                Value = s.StepValue
            }).ToList();

            var walkStepsJson = System.Text.Json.JsonSerializer.Serialize(walkStepsForJson);

            await _modelManager.UpdateRandomAdditiveWalkConfigAsync(
                request.InstrumentName,
                walkStepsJson);

            return new UpdateConfigResponse
            {
                Message = "RandomAdditiveWalk configuration updated successfully",
                Success = true
            };
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for UpdateRandomAdditiveWalkConfig");
            throw new RpcException(new Status(StatusCode.InvalidArgument, ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Instrument not found for UpdateRandomAdditiveWalkConfig");
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating RandomAdditiveWalk config for '{InstrumentName}'",
                request.InstrumentName);
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }
}
