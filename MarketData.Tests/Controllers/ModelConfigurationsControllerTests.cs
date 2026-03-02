using MarketData.Controllers;
using MarketData.Models;
using MarketData.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Text.Json;

namespace MarketData.Tests.Controllers;

public class ModelConfigurationsControllerTests
{
    private readonly Mock<IInstrumentModelManager> _mockModelManager;
    private readonly ModelConfigurationsController _controller;

    public ModelConfigurationsControllerTests()
    {
        _mockModelManager = new Mock<IInstrumentModelManager>();
        _controller = new ModelConfigurationsController(_mockModelManager.Object);
    }

    [Fact]
    public async Task GetConfigurations_WithExistingInstrument_ReturnsConfigurations()
    {
        var instrument = new Instrument
        {
            Name = "AAPL",
            ModelType = "RandomMultiplicative",
            RandomMultiplicativeConfig = new RandomMultiplicativeConfig
            {
                StandardDeviation = 0.02,
                Mean = 0.0001
            },
            MeanRevertingConfig = new MeanRevertingConfig
            {
                Mean = 100.0,
                Kappa = 0.5,
                Sigma = 2.0,
                Dt = 1.0
            },
            FlatConfig = new FlatConfig()
        };

        _mockModelManager
            .Setup(m => m.GetInstrumentWithConfigurationsAsync("AAPL"))
            .ReturnsAsync(instrument);

        var result = await _controller.GetConfigurations("AAPL");

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var json = JsonSerializer.Serialize(okResult.Value);
        var response = JsonDocument.Parse(json).RootElement;

        Assert.Equal("AAPL", response.GetProperty("Name").GetString());
        Assert.Equal("RandomMultiplicative", response.GetProperty("ActiveModel").GetString());

        var configs = response.GetProperty("Configurations");
        var randomMulti = configs.GetProperty("RandomMultiplicative");
        Assert.Equal(0.02, randomMulti.GetProperty("StandardDeviation").GetDouble());
        Assert.Equal(0.0001, randomMulti.GetProperty("Mean").GetDouble());

        var meanRev = configs.GetProperty("MeanReverting");
        Assert.Equal(100.0, meanRev.GetProperty("Mean").GetDouble());
        Assert.Equal(0.5, meanRev.GetProperty("Kappa").GetDouble());

        Assert.NotEqual(JsonValueKind.Null, configs.GetProperty("Flat").ValueKind);
    }

    [Fact]
    public async Task GetConfigurations_WithNonExistentInstrument_ReturnsNotFound()
    {
        _mockModelManager
            .Setup(m => m.GetInstrumentWithConfigurationsAsync("NONEXISTENT"))
            .ReturnsAsync((Instrument?)null);

        var result = await _controller.GetConfigurations("NONEXISTENT");

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal("Instrument 'NONEXISTENT' not found", notFoundResult.Value);
    }

    [Fact]
    public async Task SwitchModel_WithValidRequest_ReturnsOk()
    {
        var request = new SwitchModelRequest("MeanReverting");

        _mockModelManager
            .Setup(m => m.SwitchModelAsync("AAPL", "MeanReverting"))
            .ReturnsAsync("RandomMultiplicative");

        var result = await _controller.SwitchModel("AAPL", request);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var json = JsonSerializer.Serialize(okResult.Value);
        var response = JsonDocument.Parse(json).RootElement;

        Assert.NotEqual(JsonValueKind.Null, response.GetProperty("Message").ValueKind);
        Assert.Equal("RandomMultiplicative", response.GetProperty("PreviousModel").GetString());
        Assert.Equal("MeanReverting", response.GetProperty("NewModel").GetString());

        _mockModelManager.Verify(
            m => m.SwitchModelAsync("AAPL", "MeanReverting"),
            Times.Once);
    }

    [Fact]
    public async Task SwitchModel_WithInvalidModelType_ReturnsBadRequest()
    {
        var request = new SwitchModelRequest("InvalidModel");

        _mockModelManager
            .Setup(m => m.SwitchModelAsync("AAPL", "InvalidModel"))
            .ThrowsAsync(new ArgumentException("Invalid model type"));

        var result = await _controller.SwitchModel("AAPL", request);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Invalid model type", badRequestResult.Value);
    }

    [Fact]
    public async Task SwitchModel_WithNonExistentInstrument_ReturnsNotFound()
    {
        var request = new SwitchModelRequest("Flat");

        _mockModelManager
            .Setup(m => m.SwitchModelAsync("NONEXISTENT", "Flat"))
            .ThrowsAsync(new InvalidOperationException("Instrument not found"));

        var result = await _controller.SwitchModel("NONEXISTENT", request);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal("Instrument not found", notFoundResult.Value);
    }

    [Fact]
    public async Task UpdateRandomMultiplicativeConfig_WithValidData_ReturnsOk()
    {
        var request = new UpdateRandomMultiplicativeRequest(0.03, 0.0002);
        var expectedConfig = new RandomMultiplicativeConfig
        {
            StandardDeviation = 0.03,
            Mean = 0.0002
        };

        _mockModelManager
            .Setup(m => m.UpdateRandomMultiplicativeConfigAsync("AAPL", 0.03, 0.0002))
            .ReturnsAsync(expectedConfig);

        var result = await _controller.UpdateRandomMultiplicativeConfig("AAPL", request);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var json = JsonSerializer.Serialize(okResult.Value);
        var response = JsonDocument.Parse(json).RootElement;

        Assert.NotEqual(JsonValueKind.Null, response.GetProperty("Message").ValueKind);

        var config = response.GetProperty("Configuration");
        Assert.Equal(0.03, config.GetProperty("StandardDeviation").GetDouble());
        Assert.Equal(0.0002, config.GetProperty("Mean").GetDouble());

        _mockModelManager.Verify(
            m => m.UpdateRandomMultiplicativeConfigAsync("AAPL", 0.03, 0.0002),
            Times.Once);
    }

    [Fact]
    public async Task UpdateRandomMultiplicativeConfig_WithInvalidParameters_ReturnsBadRequest()
    {
        var request = new UpdateRandomMultiplicativeRequest(-0.01, 0.0);

        _mockModelManager
            .Setup(m => m.UpdateRandomMultiplicativeConfigAsync("AAPL", -0.01, 0.0))
            .ThrowsAsync(new ArgumentException("Standard deviation must be positive"));

        var result = await _controller.UpdateRandomMultiplicativeConfig("AAPL", request);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Standard deviation must be positive", badRequestResult.Value);
    }

    [Fact]
    public async Task UpdateRandomMultiplicativeConfig_WithNonExistentInstrument_ReturnsNotFound()
    {
        var request = new UpdateRandomMultiplicativeRequest(0.02, 0.0);

        _mockModelManager
            .Setup(m => m.UpdateRandomMultiplicativeConfigAsync("NONEXISTENT", 0.02, 0.0))
            .ThrowsAsync(new InvalidOperationException("Instrument not found"));

        var result = await _controller.UpdateRandomMultiplicativeConfig("NONEXISTENT", request);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal("Instrument not found", notFoundResult.Value);
    }

    [Fact]
    public async Task UpdateMeanRevertingConfig_WithValidData_ReturnsOk()
    {
        var request = new UpdateMeanRevertingRequest(150.0, 0.8, 3.0, 1.5);
        var expectedConfig = new MeanRevertingConfig
        {
            Mean = 150.0,
            Kappa = 0.8,
            Sigma = 3.0,
            Dt = 1.5
        };

        _mockModelManager
            .Setup(m => m.UpdateMeanRevertingConfigAsync("TSLA", 150.0, 0.8, 3.0, 1.5))
            .ReturnsAsync(expectedConfig);

        var result = await _controller.UpdateMeanRevertingConfig("TSLA", request);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var json = JsonSerializer.Serialize(okResult.Value);
        var response = JsonDocument.Parse(json).RootElement;

        Assert.NotEqual(JsonValueKind.Null, response.GetProperty("Message").ValueKind);

        var config = response.GetProperty("Configuration");
        Assert.Equal(150.0, config.GetProperty("Mean").GetDouble());
        Assert.Equal(0.8, config.GetProperty("Kappa").GetDouble());
        Assert.Equal(3.0, config.GetProperty("Sigma").GetDouble());
        Assert.Equal(1.5, config.GetProperty("Dt").GetDouble());

        _mockModelManager.Verify(
            m => m.UpdateMeanRevertingConfigAsync("TSLA", 150.0, 0.8, 3.0, 1.5),
            Times.Once);
    }

    [Fact]
    public async Task UpdateMeanRevertingConfig_WithInvalidParameters_ReturnsBadRequest()
    {
        var request = new UpdateMeanRevertingRequest(100.0, -0.5, 2.0, 1.0);

        _mockModelManager
            .Setup(m => m.UpdateMeanRevertingConfigAsync("TSLA", 100.0, -0.5, 2.0, 1.0))
            .ThrowsAsync(new ArgumentException("Kappa must be positive"));

        var result = await _controller.UpdateMeanRevertingConfig("TSLA", request);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Kappa must be positive", badRequestResult.Value);
    }

    [Fact]
    public async Task UpdateMeanRevertingConfig_WithNonExistentInstrument_ReturnsNotFound()
    {
        var request = new UpdateMeanRevertingRequest(100.0, 0.5, 2.0, 1.0);

        _mockModelManager
            .Setup(m => m.UpdateMeanRevertingConfigAsync("NONEXISTENT", 100.0, 0.5, 2.0, 1.0))
            .ThrowsAsync(new InvalidOperationException("Instrument not found"));

        var result = await _controller.UpdateMeanRevertingConfig("NONEXISTENT", request);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal("Instrument not found", notFoundResult.Value);
    }

    [Fact]
    public async Task SwitchModel_VerifiesCorrectParametersPassedToService()
    {
        var instrumentName = "GOOGL";
        var modelType = "Flat";
        var request = new SwitchModelRequest(modelType);

        _mockModelManager
            .Setup(m => m.SwitchModelAsync(instrumentName, modelType))
            .ReturnsAsync("RandomMultiplicative");

        await _controller.SwitchModel(instrumentName, request);

        _mockModelManager.Verify(
            m => m.SwitchModelAsync(instrumentName, modelType),
            Times.Once);
    }

    [Fact]
    public async Task UpdateRandomMultiplicativeConfig_VerifiesParametersArePassedCorrectly()
    {
        var instrumentName = "MSFT";
        var stdDev = 0.025;
        var mean = 0.0003;
        var request = new UpdateRandomMultiplicativeRequest(stdDev, mean);

        _mockModelManager
            .Setup(m => m.UpdateRandomMultiplicativeConfigAsync(instrumentName, stdDev, mean))
            .ReturnsAsync(new RandomMultiplicativeConfig { StandardDeviation = stdDev, Mean = mean });

        await _controller.UpdateRandomMultiplicativeConfig(instrumentName, request);

        _mockModelManager.Verify(
            m => m.UpdateRandomMultiplicativeConfigAsync(instrumentName, stdDev, mean),
            Times.Once);
    }

    [Fact]
    public async Task UpdateMeanRevertingConfig_VerifiesParametersArePassedCorrectly()
    {
        var instrumentName = "NVDA";
        var mean = 200.0;
        var kappa = 0.7;
        var sigma = 5.0;
        var dt = 2.0;
        var request = new UpdateMeanRevertingRequest(mean, kappa, sigma, dt);

        _mockModelManager
            .Setup(m => m.UpdateMeanRevertingConfigAsync(instrumentName, mean, kappa, sigma, dt))
            .ReturnsAsync(new MeanRevertingConfig
            {
                Mean = mean,
                Kappa = kappa,
                Sigma = sigma,
                Dt = dt
            });

        await _controller.UpdateMeanRevertingConfig(instrumentName, request);

        _mockModelManager.Verify(
            m => m.UpdateMeanRevertingConfigAsync(instrumentName, mean, kappa, sigma, dt),
            Times.Once);
    }
}
