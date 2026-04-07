using MarketData.Controllers;
using MarketData.DTO;
using MarketData.Models;
using MarketData.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestPlatform.Common;
using Moq;
using System.Text.Json;
using static MarketData.DTO.ModelConfigurationsDTO;

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
            .Setup(m => m.GetInstrumentWithConfigurationsAsync("AAPL", It.IsAny<CancellationToken>()))
            .ReturnsAsync(instrument);

        var result = await _controller.GetConfigurations("AAPL", CancellationToken.None);

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
            .Setup(m => m.GetInstrumentWithConfigurationsAsync("NONEXISTENT", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Instrument?)null);

        var result = await _controller.GetConfigurations("NONEXISTENT", CancellationToken.None);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal("Instrument 'NONEXISTENT' not found", notFoundResult.Value);
    }

    [Fact]
    public async Task SwitchModel_WithValidRequest_ReturnsOk()
    {
        _mockModelManager
            .Setup(m => m.SwitchModelAsync("AAPL", "MeanReverting", It.IsAny<CancellationToken>()))
            .ReturnsAsync("RandomMultiplicative");

        var dto = new ModelConfigurationsDTO.SwitchModelRequestDto("MeanReverting");

        var result = await _controller.SwitchModel("AAPL", dto, CancellationToken.None);

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
        _mockModelManager
            .Setup(m => m.SwitchModelAsync("AAPL", "InvalidModel", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Invalid model type"));

        var dto = new ModelConfigurationsDTO.SwitchModelRequestDto("InvalidModel");

        var result = await _controller.SwitchModel("AAPL", dto, CancellationToken.None);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Invalid model type", badRequestResult.Value);
    }

    [Fact]
    public async Task SwitchModel_WithNonExistentInstrument_ReturnsNotFound()
    {

        var dto = new ModelConfigurationsDTO.SwitchModelRequestDto("Flat");

        _mockModelManager
            .Setup(m => m.SwitchModelAsync("NONEXISTENT", "Flat", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Instrument not found"));

        var result = await _controller.SwitchModel("NONEXISTENT", dto, CancellationToken.None);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal("Instrument not found", notFoundResult.Value);
    }

    [Fact]
    public async Task UpdateRandomMultiplicativeConfig_WithValidData_ReturnsOk()
    {
        var dto = new UpdateRandomMultiplicativeRequestDto(0.03, 0.0002);


        var expectedConfig = new RandomMultiplicativeConfig
        {
            StandardDeviation = 0.03,
            Mean = 0.0002
        };


        _mockModelManager
            .Setup(m => m.UpdateRandomMultiplicativeConfigAsync("AAPL", 0.03, 0.0002, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedConfig);

        var result = await _controller.UpdateRandomMultiplicativeConfig("AAPL", dto, CancellationToken.None);

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
        var dto = new UpdateRandomMultiplicativeRequestDto(-0.01, 0.0);

        _mockModelManager
            .Setup(m => m.UpdateRandomMultiplicativeConfigAsync("AAPL", -0.01, 0.0, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Standard deviation must be positive"));

        var result = await _controller.UpdateRandomMultiplicativeConfig("AAPL", dto, CancellationToken.None);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Standard deviation must be positive", badRequestResult.Value);
    }

    [Fact]
    public async Task UpdateRandomMultiplicativeConfig_WithNonExistentInstrument_ReturnsNotFound()
    {
        var dto = new UpdateRandomMultiplicativeRequestDto(-0.02, 0.0);

        _mockModelManager
            .Setup(m => m.UpdateRandomMultiplicativeConfigAsync("NONEXISTENT", 0.02, 0.0, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Instrument not found"));

        var result = await _controller.UpdateRandomMultiplicativeConfig("NONEXISTENT", dto, CancellationToken.None);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal("Instrument not found", notFoundResult.Value);
    }

    [Fact]
    public async Task UpdateMeanRevertingConfig_WithValidData_ReturnsOk()
    {
        var dto = new UpdateMeanRevertingRequestDto(150.0, 0.8, 3.0, 1.5); 
        var expectedConfig = new MeanRevertingConfig
        {
            Mean = 150.0,
            Kappa = 0.8,
            Sigma = 3.0,
            Dt = 1.5
        };

        _mockModelManager
            .Setup(m => m.UpdateMeanRevertingConfigAsync("TSLA", 150.0, 0.8, 3.0, 1.5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedConfig);

        var result = await _controller.UpdateMeanRevertingConfig("TSLA", dto, CancellationToken.None);

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
        var dto = new UpdateMeanRevertingRequestDto(100.0, -0.5, 2.0, 1.0);

        _mockModelManager
            .Setup(m => m.UpdateMeanRevertingConfigAsync("TSLA", 100.0, -0.5, 2.0, 1.0, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Kappa must be positive"));

        var result = await _controller.UpdateMeanRevertingConfig("TSLA", dto, CancellationToken.None);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Kappa must be positive", badRequestResult.Value);
    }

    [Fact]
    public async Task UpdateMeanRevertingConfig_WithNonExistentInstrument_ReturnsNotFound()
    {
        var dto = new UpdateMeanRevertingRequestDto(100.0, 0.5, 2.0, 1.0);

        _mockModelManager
            .Setup(m => m.UpdateMeanRevertingConfigAsync("NONEXISTENT", 100.0, 0.5, 2.0, 1.0, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Instrument not found"));

        var result = await _controller.UpdateMeanRevertingConfig("NONEXISTENT", dto, CancellationToken.None);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal("Instrument not found", notFoundResult.Value);
    }

    [Fact]
    public async Task SwitchModel_VerifiesCorrectParametersPassedToService()
    {
        var instrumentName = "GOOGL";
        var modelType = "Flat";
        var dto = new ModelConfigurationsDTO.SwitchModelRequestDto(modelType);

        _mockModelManager
            .Setup(m => m.SwitchModelAsync(instrumentName, modelType, It.IsAny<CancellationToken>()))
            .ReturnsAsync("RandomMultiplicative");

        await _controller.SwitchModel(instrumentName, dto, CancellationToken.None);

        _mockModelManager.Verify(
            m => m.SwitchModelAsync(instrumentName, modelType, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateRandomMultiplicativeConfig_VerifiesParametersArePassedCorrectly()
    {
        var instrumentName = "MSFT";
        var stdDev = 0.025;
        var mean = 0.0003;
        var dto = new UpdateRandomMultiplicativeRequestDto(stdDev, mean);

        _mockModelManager
            .Setup(m => m.UpdateRandomMultiplicativeConfigAsync(instrumentName, stdDev, mean, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RandomMultiplicativeConfig { StandardDeviation = stdDev, Mean = mean });

        await _controller.UpdateRandomMultiplicativeConfig(instrumentName, dto, CancellationToken.None);

        _mockModelManager.Verify(
            m => m.UpdateRandomMultiplicativeConfigAsync(instrumentName, stdDev, mean, It.IsAny<CancellationToken>()),
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
        var dto = new UpdateMeanRevertingRequestDto(mean, kappa, sigma, dt);

        _mockModelManager
            .Setup(m => m.UpdateMeanRevertingConfigAsync(instrumentName, mean, kappa, sigma, dt, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MeanRevertingConfig
            {
                Mean = mean,
                Kappa = kappa,
                Sigma = sigma,
                Dt = dt
            });

        await _controller.UpdateMeanRevertingConfig(instrumentName, dto, CancellationToken.None);

        _mockModelManager.Verify(
            m => m.UpdateMeanRevertingConfigAsync(instrumentName, mean, kappa, sigma, dt, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetConfigurations_WithCancelledToken_PropagatesCancellation()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        _mockModelManager
            .Setup(m => m.GetInstrumentWithConfigurationsAsync("AAPL", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _controller.GetConfigurations("AAPL", cts.Token));

        _mockModelManager.Verify(
            m => m.GetInstrumentWithConfigurationsAsync("AAPL", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateRandomMultiplicativeConfig_WithCancelledToken_PropagatesCancellation()
    {
        var dto = new UpdateRandomMultiplicativeRequestDto(0.03, 0.0002);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        _mockModelManager
            .Setup(m => m.UpdateRandomMultiplicativeConfigAsync("AAPL", 0.03, 0.0002, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _controller.UpdateRandomMultiplicativeConfig("AAPL", dto, cts.Token));

        _mockModelManager.Verify(
            m => m.UpdateRandomMultiplicativeConfigAsync("AAPL", 0.03, 0.0002, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
