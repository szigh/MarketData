using MarketData.Data;
using MarketData.Models;
using MarketData.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace MarketData.Tests.Services;

public class InstrumentModelManagerTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly MarketDataContext _context;
    private readonly Mock<IPriceSimulatorFactory> _mockFactory;

    //system under test
    private readonly InstrumentModelManager _manager;

    public InstrumentModelManagerTests()
    {
        var services = new ServiceCollection();

        var options = new DbContextOptionsBuilder<MarketDataContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
            .Options;

        _context = new MarketDataContext(options);
        services.AddScoped<MarketDataContext>(_ => new MarketDataContext(options));

        _serviceProvider = services.BuildServiceProvider();
        
        _mockFactory = new Mock<IPriceSimulatorFactory>();
        _manager = new InstrumentModelManager(
            _serviceProvider,
            _mockFactory.Object,
            NullLogger<InstrumentModelManager>.Instance);
    }

    [Fact]
    public async Task GetInstrumentWithConfigurationsAsync_WithExistingInstrument_ReturnsInstrument()
    {
        var instrument = new Instrument
        {
            Name = "AAPL",
            ModelType = "RandomMultiplicative",
            TickIntervalMillieconds = 1000
        };
        _context.Instruments.Add(instrument);
        await _context.SaveChangesAsync();

        var result = await _manager.GetInstrumentWithConfigurationsAsync("AAPL");

        Assert.NotNull(result);
        Assert.Equal("AAPL", result.Name);
        Assert.Equal("RandomMultiplicative", result.ModelType);
    }

    [Fact]
    public async Task GetInstrumentWithConfigurationsAsync_WithNonExistentInstrument_ReturnsNull()
    {
        var result = await _manager.GetInstrumentWithConfigurationsAsync("NONEXISTENT");

        Assert.Null(result);
    }

    [Fact]
    public async Task SwitchModelAsync_WithValidModelType_SwitchesModel()
    {
        _context.Instruments.Add(new Instrument
        {
            Name = "AAPL",
            ModelType = "RandomMultiplicative",
            TickIntervalMillieconds = 1000
        });
        await _context.SaveChangesAsync();

        var previousModel = await _manager.SwitchModelAsync("AAPL", "Flat");

        Assert.Equal("RandomMultiplicative", previousModel);
    }

    [Fact]
    public async Task SwitchModelAsync_WithInvalidModelType_ThrowsArgumentException()
    {
        var instrument = new Instrument
        {
            Name = "AAPL",
            ModelType = "Flat",
            TickIntervalMillieconds = 1000
        };
        _context.Instruments.Add(instrument);
        await _context.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => _manager.SwitchModelAsync("AAPL", "InvalidModel"));

        Assert.Contains("Invalid model type", ex.Message);
        Assert.Contains("InvalidModel", ex.Message);
    }

    [Fact]
    public async Task SwitchModelAsync_WithNonExistentInstrument_ThrowsInvalidOperationException()
    {
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _manager.SwitchModelAsync("NONEXISTENT", "Flat"));

        Assert.Contains("not found", ex.Message);
    }

    [Fact]
    public async Task SwitchModelAsync_RaisesConfigurationChangedEvent()
    {
        var instrument = new Instrument
        {
            Name = "AAPL",
            ModelType = "Flat",
            TickIntervalMillieconds = 1000
        };
        _context.Instruments.Add(instrument);
        await _context.SaveChangesAsync();

        ModelConfigurationChangedEventArgs? eventArgs = null;
        _manager.ConfigurationChanged += (sender, args) => eventArgs = args;

        await _manager.SwitchModelAsync("AAPL", "MeanReverting");

        Assert.NotNull(eventArgs);
        Assert.Equal("AAPL", eventArgs.InstrumentName);
        Assert.Equal("MeanReverting", eventArgs.ModelType);
    }

    [Fact]
    public async Task UpdateRandomMultiplicativeConfigAsync_CreatesNewConfig_WhenNoneExists()
    {
        var instrument = new Instrument
        {
            Name = "AAPL",
            ModelType = "RandomMultiplicative",
            TickIntervalMillieconds = 1000
        };
        _context.Instruments.Add(instrument);
        await _context.SaveChangesAsync();

        var config = await _manager.UpdateRandomMultiplicativeConfigAsync("AAPL", 0.03, 0.0002);

        Assert.NotNull(config);
        Assert.Equal(0.03, config.StandardDeviation);
        Assert.Equal(0.0002, config.Mean);
        Assert.Equal(instrument.Id, config.InstrumentId);
    }

    [Fact]
    public async Task UpdateRandomMultiplicativeConfigAsync_UpdatesExistingConfig()
    {
        var instrument = new Instrument
        {
            Name = "AAPL",
            ModelType = "RandomMultiplicative",
            TickIntervalMillieconds = 1000,
            RandomMultiplicativeConfig = new RandomMultiplicativeConfig
            {
                StandardDeviation = 0.02,
                Mean = 0.0001
            }
        };
        _context.Instruments.Add(instrument);
        await _context.SaveChangesAsync();

        var config = await _manager.UpdateRandomMultiplicativeConfigAsync("AAPL", 0.05, 0.0005);

        Assert.Equal(0.05, config.StandardDeviation);
        Assert.Equal(0.0005, config.Mean);
    }

    [Fact]
    public async Task UpdateRandomMultiplicativeConfigAsync_WithNegativeStdDev_ThrowsArgumentException()
    {
        var instrument = new Instrument
        {
            Name = "AAPL",
            ModelType = "RandomMultiplicative",
            TickIntervalMillieconds = 1000
        };
        _context.Instruments.Add(instrument);
        await _context.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => _manager.UpdateRandomMultiplicativeConfigAsync("AAPL", -0.01, 0.0));

        Assert.Contains("Standard deviation must be positive", ex.Message);
    }

    [Fact]
    public async Task UpdateRandomMultiplicativeConfigAsync_WithZeroStdDev_ThrowsArgumentException()
    {
        var instrument = new Instrument
        {
            Name = "AAPL",
            ModelType = "RandomMultiplicative",
            TickIntervalMillieconds = 1000
        };
        _context.Instruments.Add(instrument);
        await _context.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => _manager.UpdateRandomMultiplicativeConfigAsync("AAPL", 0.0, 0.0));

        Assert.Contains("Standard deviation must be positive", ex.Message);
    }

    [Fact]
    public async Task UpdateRandomMultiplicativeConfigAsync_RaisesConfigurationChangedEvent()
    {
        var instrument = new Instrument
        {
            Name = "AAPL",
            ModelType = "RandomMultiplicative",
            TickIntervalMillieconds = 1000
        };
        _context.Instruments.Add(instrument);
        await _context.SaveChangesAsync();

        ModelConfigurationChangedEventArgs? eventArgs = null;
        _manager.ConfigurationChanged += (sender, args) => eventArgs = args;

        await _manager.UpdateRandomMultiplicativeConfigAsync("AAPL", 0.02, 0.0);

        Assert.NotNull(eventArgs);
        Assert.Equal("AAPL", eventArgs.InstrumentName);
    }

    [Fact]
    public async Task UpdateMeanRevertingConfigAsync_CreatesNewConfig_WhenNoneExists()
    {
        var instrument = new Instrument
        {
            Name = "AAPL",
            ModelType = "MeanReverting",
            TickIntervalMillieconds = 1000
        };
        _context.Instruments.Add(instrument);
        await _context.SaveChangesAsync();

        var config = await _manager.UpdateMeanRevertingConfigAsync("AAPL", 150.0, 0.8, 3.0, 1.5);

        Assert.NotNull(config);
        Assert.Equal(150.0, config.Mean);
        Assert.Equal(0.8, config.Kappa);
        Assert.Equal(3.0, config.Sigma);
        Assert.Equal(1.5, config.Dt);
    }

    [Fact]
    public async Task UpdateMeanRevertingConfigAsync_UpdatesExistingConfig()
    {
        var instrument = new Instrument
        {
            Name = "AAPL",
            ModelType = "MeanReverting",
            TickIntervalMillieconds = 1000,
            MeanRevertingConfig = new MeanRevertingConfig
            {
                Mean = 100.0,
                Kappa = 0.5,
                Sigma = 2.0,
                Dt = 1.0
            }
        };
        _context.Instruments.Add(instrument);
        await _context.SaveChangesAsync();

        var config = await _manager.UpdateMeanRevertingConfigAsync("AAPL", 200.0, 0.9, 4.0, 2.0);

        Assert.Equal(200.0, config.Mean);
        Assert.Equal(0.9, config.Kappa);
        Assert.Equal(4.0, config.Sigma);
        Assert.Equal(2.0, config.Dt);
    }

    [Fact]
    public async Task UpdateMeanRevertingConfigAsync_WithNegativeKappa_ThrowsArgumentException()
    {
        var instrument = new Instrument
        {
            Name = "AAPL",
            ModelType = "MeanReverting",
            TickIntervalMillieconds = 1000
        };
        _context.Instruments.Add(instrument);
        await _context.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => _manager.UpdateMeanRevertingConfigAsync("AAPL", 100.0, -0.5, 2.0, 1.0));

        Assert.Contains("Kappa", ex.Message);
        Assert.Contains("must be positive", ex.Message);
    }

    [Fact]
    public async Task UpdateMeanRevertingConfigAsync_WithNegativeSigma_ThrowsArgumentException()
    {
        var instrument = new Instrument
        {
            Name = "AAPL",
            ModelType = "MeanReverting",
            TickIntervalMillieconds = 1000
        };
        _context.Instruments.Add(instrument);
        await _context.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => _manager.UpdateMeanRevertingConfigAsync("AAPL", 100.0, 0.5, -2.0, 1.0));

        Assert.Contains("Sigma", ex.Message);
        Assert.Contains("cannot be negative", ex.Message);
    }

    [Fact]
    public async Task UpdateMeanRevertingConfigAsync_WithNegativeDt_ThrowsArgumentException()
    {
        var instrument = new Instrument
        {
            Name = "AAPL",
            ModelType = "MeanReverting",
            TickIntervalMillieconds = 1000
        };
        _context.Instruments.Add(instrument);
        await _context.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => _manager.UpdateMeanRevertingConfigAsync("AAPL", 100.0, 0.5, 2.0, -1.0));

        Assert.Contains("Dt", ex.Message);
        Assert.Contains("must be positive", ex.Message);
    }

    [Fact]
    public async Task UpdateMeanRevertingConfigAsync_RaisesConfigurationChangedEvent()
    {
        var instrument = new Instrument
        {
            Name = "AAPL",
            ModelType = "MeanReverting",
            TickIntervalMillieconds = 1000
        };
        _context.Instruments.Add(instrument);
        await _context.SaveChangesAsync();

        ModelConfigurationChangedEventArgs? eventArgs = null;
        _manager.ConfigurationChanged += (sender, args) => eventArgs = args;

        await _manager.UpdateMeanRevertingConfigAsync("AAPL", 150.0, 0.8, 3.0, 1.5);

        Assert.NotNull(eventArgs);
        Assert.Equal("AAPL", eventArgs.InstrumentName);
    }

    [Fact]
    public async Task UpdateRandomAdditiveWalkConfigAsync_WithValidJson_CreatesConfig()
    {
        var instrument = new Instrument
        {
            Name = "AAPL",
            ModelType = "RandomAdditiveWalk",
            TickIntervalMillieconds = 1000
        };
        _context.Instruments.Add(instrument);
        await _context.SaveChangesAsync();

        var walkStepsJson = """
            [
                {"Probability": 0.5, "Value": 1.0},
                {"Probability": 0.5, "Value": -1.0}
            ]
            """;

        var config = await _manager.UpdateRandomAdditiveWalkConfigAsync("AAPL", walkStepsJson);

        Assert.NotNull(config);
        Assert.NotNull(config.WalkStepsJson);
    }

    [Fact]
    public async Task UpdateRandomAdditiveWalkConfigAsync_WithInvalidJson_ThrowsArgumentException()
    {
        var instrument = new Instrument
        {
            Name = "AAPL",
            ModelType = "RandomAdditiveWalk",
            TickIntervalMillieconds = 1000
        };
        _context.Instruments.Add(instrument);
        await _context.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => _manager.UpdateRandomAdditiveWalkConfigAsync("AAPL", "not valid json"));

        Assert.Contains("Invalid walk steps JSON", ex.Message);
    }

    [Fact]
    public async Task UpdateRandomAdditiveWalkConfigAsync_WithEmptySteps_ThrowsArgumentException()
    {
        var instrument = new Instrument
        {
            Name = "AAPL",
            ModelType = "RandomAdditiveWalk",
            TickIntervalMillieconds = 1000
        };
        _context.Instruments.Add(instrument);
        await _context.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => _manager.UpdateRandomAdditiveWalkConfigAsync("AAPL", "[]"));

        Assert.Contains("cannot be empty", ex.Message);
    }

    [Fact]
    public async Task UpdateRandomAdditiveWalkConfigAsync_WithInvalidProbabilities_ThrowsArgumentException()
    {
        var instrument = new Instrument
        {
            Name = "AAPL",
            ModelType = "RandomAdditiveWalk",
            TickIntervalMillieconds = 1000
        };
        _context.Instruments.Add(instrument);
        await _context.SaveChangesAsync();

        var walkStepsJson = """
            [
                {"Probability": 0.6, "Value": 1.0},
                {"Probability": 0.6, "Value": -1.0}
            ]
            """;

        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => _manager.UpdateRandomAdditiveWalkConfigAsync("AAPL", walkStepsJson));

        Assert.Contains("walkStepsJson", ex.ParamName);
    }

    [Fact]
    public async Task UpdateRandomAdditiveWalkConfigAsync_RaisesConfigurationChangedEvent()
    {
        var instrument = new Instrument
        {
            Name = "AAPL",
            ModelType = "RandomAdditiveWalk",
            TickIntervalMillieconds = 1000
        };
        _context.Instruments.Add(instrument);
        await _context.SaveChangesAsync();

        ModelConfigurationChangedEventArgs? eventArgs = null;
        _manager.ConfigurationChanged += (sender, args) => eventArgs = args;

        var walkStepsJson = """
            [
                {"Probability": 0.5, "Value": 1.0},
                {"Probability": 0.5, "Value": -1.0}
            ]
            """;

        await _manager.UpdateRandomAdditiveWalkConfigAsync("AAPL", walkStepsJson);

        Assert.NotNull(eventArgs);
        Assert.Equal("AAPL", eventArgs.InstrumentName);
    }

    [Fact]
    public void IsValidModelType_WithValidTypes_ReturnsTrue()
    {
        Assert.True(InstrumentModelManager.IsValidModelType("RandomMultiplicative"));
        Assert.True(InstrumentModelManager.IsValidModelType("MeanReverting"));
        Assert.True(InstrumentModelManager.IsValidModelType("Flat"));
        Assert.True(InstrumentModelManager.IsValidModelType("RandomAdditiveWalk"));
    }

    [Fact]
    public void IsValidModelType_WithInvalidType_ReturnsFalse()
    {
        Assert.False(InstrumentModelManager.IsValidModelType("InvalidModel"));
        Assert.False(InstrumentModelManager.IsValidModelType(""));
    }

    [Fact]
    public void GetSupportedModelTypes_ReturnsAllExpectedTypes()
    {
        var types = InstrumentModelManager.GetSupportedModelTypes();

        Assert.Equal(4, types.Length);
        Assert.Contains("RandomMultiplicative", types);
        Assert.Contains("MeanReverting", types);
        Assert.Contains("Flat", types);
        Assert.Contains("RandomAdditiveWalk", types);
    }

    [Fact]
    public async Task EnsureModelTypeAsync_WithNullModelType_SetsDefaultModel()
    {
        using var scope = _serviceProvider.CreateScope();
        var scopedContext = scope.ServiceProvider.GetRequiredService<MarketDataContext>();

        var instrument = new Instrument
        {
            Name = "AAPL",
            ModelType = "RandomMultiplicative",
            TickIntervalMillieconds = 1000
        };
        scopedContext.Instruments.Add(instrument);
        await scopedContext.SaveChangesAsync();

        instrument.ModelType = null!;

        var changed = await _manager.EnsureModelTypeAsync(instrument, scopedContext);

        Assert.True(changed);
        Assert.Equal("Flat", instrument.ModelType);
    }

    [Fact]
    public async Task EnsureModelTypeAsync_WithEmptyModelType_SetsDefaultModel()
    {
        using var scope = _serviceProvider.CreateScope();
        var scopedContext = scope.ServiceProvider.GetRequiredService<MarketDataContext>();

        var instrument = new Instrument
        {
            Name = "AAPL",
            ModelType = "RandomMultiplicative",
            TickIntervalMillieconds = 1000
        };
        scopedContext.Instruments.Add(instrument);
        await scopedContext.SaveChangesAsync();

        instrument.ModelType = "";

        var changed = await _manager.EnsureModelTypeAsync(instrument, scopedContext);

        Assert.True(changed);
        Assert.Equal("Flat", instrument.ModelType);
    }

    [Fact]
    public async Task EnsureModelTypeAsync_WithValidModelType_DoesNotChange()
    {
        using var scope = _serviceProvider.CreateScope();
        var scopedContext = scope.ServiceProvider.GetRequiredService<MarketDataContext>();

        var instrument = new Instrument
        {
            Name = "AAPL",
            ModelType = "RandomMultiplicative",
            TickIntervalMillieconds = 1000
        };
        scopedContext.Instruments.Add(instrument);
        await scopedContext.SaveChangesAsync();

        var changed = await _manager.EnsureModelTypeAsync(instrument, scopedContext);

        Assert.False(changed);
        Assert.Equal("RandomMultiplicative", instrument.ModelType);
    }

    [Fact]
    public async Task LoadAndInitializeAllInstrumentsAsync_LoadsAllInstruments()
    {
        _context.Instruments.AddRange(
            new Instrument { Name = "AAPL", ModelType = "Flat", TickIntervalMillieconds = 1000 },
            new Instrument { Name = "GOOGL", ModelType = "RandomMultiplicative", TickIntervalMillieconds = 2000 },
            new Instrument { Name = "MSFT", ModelType = "MeanReverting", TickIntervalMillieconds = 1500 }
        );
        await _context.SaveChangesAsync();

        var instruments = await _manager.LoadAndInitializeAllInstrumentsAsync();

        Assert.Equal(3, instruments.Count);
        Assert.True(instruments.ContainsKey("AAPL"));
        Assert.True(instruments.ContainsKey("GOOGL"));
        Assert.True(instruments.ContainsKey("MSFT"));
        Assert.NotNull(instruments["AAPL"]);
        Assert.Equal("Flat", instruments["AAPL"].ModelType);
        Assert.Equal(1000, instruments["AAPL"].TickIntervalMillieconds);
    }

    [Fact]
    public async Task UpdateTickIntervalAsync_UpdatesInterval()
    {
        _context.Instruments.Add(new Instrument
        {
            Name = "AAPL",
            ModelType = "Flat",
            TickIntervalMillieconds = 1000
        });
        await _context.SaveChangesAsync();

        var result = await _manager.UpdateTickIntervalAsync("AAPL", 2500);

        Assert.Equal(2500, result);

        //reload
        var instruments = await _manager.LoadAndInitializeAllInstrumentsAsync();
        Assert.True(instruments.ContainsKey("AAPL"));
        Assert.NotNull(instruments["AAPL"]);
        Assert.Equal(2500, instruments["AAPL"].TickIntervalMillieconds);
    }

    [Fact]
    public async Task UpdateTickIntervalAsync_WithNonExistentInstrument_ThrowsInvalidOperationException()
    {
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _manager.UpdateTickIntervalAsync("NONEXISTENT", 1000));

        Assert.Contains("not found", ex.Message);
    }

    [Fact]
    public async Task UpdateTickIntervalAsync_WithNegativeInterval_ThrowsArgumentException()
    {
        var instrument = new Instrument
        {
            Name = "AAPL",
            ModelType = "Flat",
            TickIntervalMillieconds = 1000
        };
        _context.Instruments.Add(instrument);
        await _context.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => _manager.UpdateTickIntervalAsync("AAPL", -500));

        Assert.Contains("Tick interval must be a positive integer", ex.Message);
    }

    public void Dispose()
    {
        _context.Dispose();
        _serviceProvider.Dispose();
    }
}
