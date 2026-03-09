using MarketData.Grpc;

namespace MarketData.Tests.Grpc;

/// <summary>
/// Simple contract validation tests that verify proto message structure
/// without requiring a running server. These tests catch breaking changes
/// to message definitions at compile time.
/// </summary>
public class ProtoContractValidationTests
{
    [Fact]
    public void PriceUpdate_MaintainsFieldStructure()
    {
        var update = new PriceUpdate
        {
            Instrument = "AAPL",
            Value = 150.25,
            Timestamp = DateTime.UtcNow.Ticks
        };

        Assert.Equal("AAPL", update.Instrument);
        Assert.Equal(150.25, update.Value);
        Assert.NotEqual(0, update.Timestamp);
    }

    [Fact]
    public void SubscribeRequest_SupportsMultipleInstruments()
    {
        var request = new SubscribeRequest();
        request.Instruments.Add("AAPL");
        request.Instruments.Add("GOOGL");
        request.Instruments.Add("MSFT");

        Assert.Equal(3, request.Instruments.Count);
        Assert.Contains("GOOGL", request.Instruments);
    }

    [Fact]
    public void HistoricalDataRequest_HasRequiredFields()
    {
        var request = new HistoricalDataRequest
        {
            Instrument = "TEST",
            StartTimestamp = 100,
            EndTimestamp = 200
        };

        Assert.Equal("TEST", request.Instrument);
        Assert.Equal(100, request.StartTimestamp);
        Assert.Equal(200, request.EndTimestamp);
    }

    [Fact]
    public void HistoricalDataResponse_SupportsMultiplePrices()
    {
        var response = new HistoricalDataResponse();
        response.Prices.Add(new PriceUpdate { Instrument = "TEST", Value = 100, Timestamp = 1 });
        response.Prices.Add(new PriceUpdate { Instrument = "TEST", Value = 101, Timestamp = 2 });

        Assert.Equal(2, response.Prices.Count);
        Assert.All(response.Prices, p => Assert.Equal("TEST", p.Instrument));
    }

    [Fact]
    public void ConfigurationsResponse_HasAllExpectedFields()
    {
        var response = new ConfigurationsResponse
        {
            InstrumentName = "AAPL",
            ActiveModel = "RandomMultiplicative",
            TickIntervalMs = 1000,
            FlatConfigured = true
        };

        Assert.Equal("AAPL", response.InstrumentName);
        Assert.Equal("RandomMultiplicative", response.ActiveModel);
        Assert.Equal(1000, response.TickIntervalMs);
        Assert.True(response.FlatConfigured);
    }

    [Fact]
    public void ConfigurationsResponse_SupportsAllConfigTypes()
    {
        var response = new ConfigurationsResponse
        {
            InstrumentName = "TEST",
            ActiveModel = "RandomMultiplicative"
        };

        response.RandomMultiplicative = new RandomMultiplicativeConfigData
        {
            StandardDeviation = 0.02,
            Mean = 0.0001
        };

        response.MeanReverting = new MeanRevertingConfigData
        {
            Mean = 100.0,
            Kappa = 0.5,
            Sigma = 2.0,
            Dt = 1.0
        };

        response.RandomAdditiveWalk = new RandomAdditiveWalkConfigData();
        response.RandomAdditiveWalk.WalkSteps.Add(new WalkStep
        {
            Probability = 0.5,
            StepValue = 1.0
        });

        Assert.NotNull(response.RandomMultiplicative);
        Assert.Equal(0.02, response.RandomMultiplicative.StandardDeviation);

        Assert.NotNull(response.MeanReverting);
        Assert.Equal(100.0, response.MeanReverting.Mean);

        Assert.NotNull(response.RandomAdditiveWalk);
        Assert.Single(response.RandomAdditiveWalk.WalkSteps);
    }

    [Fact]
    public void RandomMultiplicativeConfigData_MaintainsStructure()
    {
        var config = new RandomMultiplicativeConfigData
        {
            StandardDeviation = 0.03,
            Mean = 0.0002
        };

        Assert.Equal(0.03, config.StandardDeviation);
        Assert.Equal(0.0002, config.Mean);
    }

    [Fact]
    public void MeanRevertingConfigData_MaintainsStructure()
    {
        var config = new MeanRevertingConfigData
        {
            Mean = 150.0,
            Kappa = 0.8,
            Sigma = 3.0,
            Dt = 1.5
        };

        Assert.Equal(150.0, config.Mean);
        Assert.Equal(0.8, config.Kappa);
        Assert.Equal(3.0, config.Sigma);
        Assert.Equal(1.5, config.Dt);
    }

    [Fact]
    public void WalkStep_MaintainsStructure()
    {
        var step = new WalkStep
        {
            Probability = 0.33,
            StepValue = -2.5
        };

        Assert.Equal(0.33, step.Probability);
        Assert.Equal(-2.5, step.StepValue);
    }

    [Fact]
    public void SwitchModelRequest_HasRequiredFields()
    {
        var request = new SwitchModelRequest
        {
            InstrumentName = "AAPL",
            ModelType = "Flat"
        };

        Assert.Equal("AAPL", request.InstrumentName);
        Assert.Equal("Flat", request.ModelType);
    }

    [Fact]
    public void SwitchModelResponse_HasAllFields()
    {
        var response = new SwitchModelResponse
        {
            Message = "Success",
            PreviousModel = "RandomMultiplicative",
            NewModel = "Flat"
        };

        Assert.Equal("Success", response.Message);
        Assert.Equal("RandomMultiplicative", response.PreviousModel);
        Assert.Equal("Flat", response.NewModel);
    }

    [Fact]
    public void UpdateConfigResponse_HasRequiredFields()
    {
        var response = new UpdateConfigResponse
        {
            Message = "Config updated",
            Success = true
        };

        Assert.Equal("Config updated", response.Message);
        Assert.True(response.Success);
    }

    [Fact]
    public void UpdateRandomMultiplicativeRequest_MaintainsStructure()
    {
        var request = new UpdateRandomMultiplicativeRequest
        {
            InstrumentName = "TEST",
            StandardDeviation = 0.025,
            Mean = 0.0003
        };

        Assert.Equal("TEST", request.InstrumentName);
        Assert.Equal(0.025, request.StandardDeviation);
        Assert.Equal(0.0003, request.Mean);
    }

    [Fact]
    public void UpdateMeanRevertingRequest_MaintainsStructure()
    {
        var request = new UpdateMeanRevertingRequest
        {
            InstrumentName = "TEST",
            Mean = 200.0,
            Kappa = 0.7,
            Sigma = 5.0,
            Dt = 2.0
        };

        Assert.Equal("TEST", request.InstrumentName);
        Assert.Equal(200.0, request.Mean);
        Assert.Equal(0.7, request.Kappa);
        Assert.Equal(5.0, request.Sigma);
        Assert.Equal(2.0, request.Dt);
    }

    [Fact]
    public void UpdateRandomAdditiveWalkRequest_SupportsWalkSteps()
    {
        var request = new UpdateRandomAdditiveWalkRequest
        {
            InstrumentName = "TEST"
        };
        request.WalkSteps.Add(new WalkStep { Probability = 0.33, StepValue = 1.0 });
        request.WalkSteps.Add(new WalkStep { Probability = 0.33, StepValue = 0.0 });
        request.WalkSteps.Add(new WalkStep { Probability = 0.34, StepValue = -1.0 });

        Assert.Equal("TEST", request.InstrumentName);
        Assert.Equal(3, request.WalkSteps.Count);
        Assert.All(request.WalkSteps, step => Assert.InRange(step.Probability, 0, 1));
    }

    [Fact]
    public void GetSupportedModelsRequest_Exists()
    {
        var request = new GetSupportedModelsRequest();
        Assert.NotNull(request);
    }

    [Fact]
    public void SupportedModelsResponse_SupportsMultipleModels()
    {
        var response = new SupportedModelsResponse();
        response.SupportedModels.Add("RandomMultiplicative");
        response.SupportedModels.Add("MeanReverting");
        response.SupportedModels.Add("Flat");
        response.SupportedModels.Add("RandomAdditiveWalk");

        Assert.Equal(4, response.SupportedModels.Count);
        Assert.Contains("Flat", response.SupportedModels);
    }

    [Fact]
    public void TryAddInstrumentRequestTest()
    {
        var request = new TryAddInstrumentRequest();
        request.InstrumentName = "TEST";
        request.TickIntervalMs = 1000;
        Assert.NotNull(request);
        Assert.Equal("TEST", request.InstrumentName);
        Assert.Equal(1000, request.TickIntervalMs);
    }

    [Fact]
    public void TryAddInstrumentResponseTest() 
    {
        var response = new TryAddInstrumentResponse();
        response.Message = "Instrument added successfully";
        response.Added = true;
        Assert.NotNull(response);
        Assert.Equal("Instrument added successfully", response.Message);
        Assert.True(response.Added);
    }

    [Fact]
    public void TryRemoveInstrumentRequestTest()
    {
        var request = new TryRemoveInstrumentRequest();
        request.InstrumentName = "TEST";
        Assert.NotNull(request);
        Assert.Equal("TEST", request.InstrumentName);
    }

    [Fact]
    public void TryRemoveInstrumentResponseTest()
    {
        var response = new TryRemoveInstrumentResponse();
        response.Message = "Instrument removed successfully";
        response.Removed = true;
        Assert.NotNull(response);
        Assert.Equal("Instrument removed successfully", response.Message);
        Assert.True(response.Removed);
    }

    [Fact]
    public void GetAllInstrumentsRequestTest()
    {
        var request = new GetAllInstrumentsRequest();
        Assert.NotNull(request);
    }

    [Fact]
    public void GetAllInstrumentsResponseTest()
    {
        var response = new GetAllInstrumentsResponse();
        response.Configurations.Add(new ConfigurationsResponse
        {
            InstrumentName = "TEST",
            ActiveModel = "RandomMultiplicative",
            TickIntervalMs = 1000,
            FlatConfigured = false
        });
        response.Configurations.Add(new ConfigurationsResponse
        {
            InstrumentName = "AAPL",
            ActiveModel = "MeanReverting",
            TickIntervalMs = 500,
            FlatConfigured = true,
            MeanReverting = new MeanRevertingConfigData
            {
                Mean = 150.0,
                Kappa = 0.5,
                Sigma = 2.0,
                Dt = 1.0
            }
        });

        Assert.Equal(2, response.Configurations.Count);
        Assert.Equal("TEST", response.Configurations[0].InstrumentName);
        Assert.Equal("AAPL", response.Configurations[1].InstrumentName);
        Assert.Equal("RandomMultiplicative", response.Configurations[0].ActiveModel);
        Assert.Equal("MeanReverting", response.Configurations[1].ActiveModel);  
        Assert.Equal(150.0, response.Configurations[1].MeanReverting.Mean);
        Assert.Equal(0.5, response.Configurations[1].MeanReverting.Kappa);
        Assert.Equal(2.0, response.Configurations[1].MeanReverting.Sigma);
        Assert.Equal(1.0, response.Configurations[1].MeanReverting.Dt); 
    }
}