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
            NullLogger<InstrumentModelManager>.Instance,
            new DefaultModelConfigFactory(NullLogger<DefaultModelConfigFactory>.Instance));
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
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await _manager.GetInstrumentWithConfigurationsAsync("AAPL", TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal("AAPL", result.Name);
        Assert.Equal("RandomMultiplicative", result.ModelType);
    }

    [Fact]
    public async Task GetInstrumentWithConfigurationsAsync_WithNonExistentInstrument_ReturnsNull()
    {
        var result = await _manager.GetInstrumentWithConfigurationsAsync("NONEXISTENT", TestContext.Current.CancellationToken);

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
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var previousModel = await _manager.SwitchModelAsync("AAPL", "Flat", TestContext.Current.CancellationToken);

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
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => _manager.SwitchModelAsync("AAPL", "InvalidModel", TestContext.Current.CancellationToken));

        Assert.Contains("Invalid model type", ex.Message);
        Assert.Contains("InvalidModel", ex.Message);
    }

    [Fact]
    public async Task SwitchModelAsync_WithNonExistentInstrument_ThrowsInvalidOperationException()
    {
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _manager.SwitchModelAsync("NONEXISTENT", "Flat", TestContext.Current.CancellationToken));

        Assert.Contains("not found", ex.Message);
    }

    [Fact]
    public async Task SwitchModelAsync_RaisesEvent()
    {
        var instrument = new Instrument
        {
            Name = "AAPL",
            ModelType = "Flat",
            TickIntervalMillieconds = 1000
        };
        _context.Instruments.Add(instrument);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        ModelConfigurationChangedEventArgs? eventArgs = null;
        _manager.ModelSwitched += (sender, args) => eventArgs = args;

        await _manager.SwitchModelAsync("AAPL", "MeanReverting", TestContext.Current.CancellationToken);

        Assert.NotNull(eventArgs);
        Assert.Equal("AAPL", eventArgs.InstrumentName);
        Assert.Equal("MeanReverting", eventArgs.NewModelType);
    }

    [Fact]
    public async Task TryRemoveInstrument_RaisesEvent()
    {
        var instrument = new Instrument
        {
            Name = "AAPL",
            ModelType = "Flat",
            TickIntervalMillieconds = 1000
        };
        _context.Instruments.Add(instrument);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        ModelConfigurationChangedEventArgs? eventArgs = null;
        _manager.InstrumentRemoved += (sender, args) => eventArgs = args;

        await _manager.TryRemoveInstrumentAsync("AAPL", TestContext.Current.CancellationToken);

        Assert.NotNull(eventArgs);
        Assert.Equal("AAPL", eventArgs.InstrumentName);
    }

    [Fact]
    public async Task UpdateTickIntervalAsync_RaisesEvent()
    {
        var instrument = new Instrument
        {
            Name = "AAPL",
            ModelType = "Flat",
            TickIntervalMillieconds = 1000
        };
        _context.Instruments.Add(instrument);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        ModelConfigurationChangedEventArgs? eventArgs = null;
        _manager.TickIntervalChanged += (sender, args) => eventArgs = args;

        await _manager.UpdateTickIntervalAsync("AAPL", 2000, TestContext.Current.CancellationToken);

        Assert.NotNull(eventArgs);
        Assert.Equal("AAPL", eventArgs.InstrumentName);
        Assert.Equal(2000, eventArgs.NewTickIntervalMs);
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
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var config = await _manager.UpdateRandomMultiplicativeConfigAsync("AAPL", 0.03, 0.0002, TestContext.Current.CancellationToken);

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
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var config = await _manager.UpdateRandomMultiplicativeConfigAsync("AAPL", 0.05, 0.0005, TestContext.Current.CancellationToken);

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
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => _manager.UpdateRandomMultiplicativeConfigAsync("AAPL", -0.01, 0.0, TestContext.Current.CancellationToken));

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
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => _manager.UpdateRandomMultiplicativeConfigAsync("AAPL", 0.0, 0.0, TestContext.Current.CancellationToken));

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
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        ModelConfigurationChangedEventArgs? eventArgs = null;
        _manager.ConfigurationChanged += (sender, args) => eventArgs = args;

        await _manager.UpdateRandomMultiplicativeConfigAsync("AAPL", 0.02, 0.0, TestContext.Current.CancellationToken);

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
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var config = await _manager.UpdateMeanRevertingConfigAsync("AAPL", 150.0, 0.8, 3.0, 1.5, TestContext.Current.CancellationToken);

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
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var config = await _manager.UpdateMeanRevertingConfigAsync("AAPL", 200.0, 0.9, 4.0, 2.0, TestContext.Current.CancellationToken);

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
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => _manager.UpdateMeanRevertingConfigAsync("AAPL", 100.0, -0.5, 2.0, 1.0, TestContext.Current.CancellationToken));

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
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => _manager.UpdateMeanRevertingConfigAsync("AAPL", 100.0, 0.5, -2.0, 1.0, TestContext.Current.CancellationToken));

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
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => _manager.UpdateMeanRevertingConfigAsync("AAPL", 100.0, 0.5, 2.0, -1.0, TestContext.Current.CancellationToken));

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
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        ModelConfigurationChangedEventArgs? eventArgs = null;
        _manager.ConfigurationChanged += (sender, args) => eventArgs = args;

        await _manager.UpdateMeanRevertingConfigAsync("AAPL", 150.0, 0.8, 3.0, 1.5, TestContext.Current.CancellationToken);

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
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var walkStepsJson = """
            [
                {"Probability": 0.5, "Value": 1.0},
                {"Probability": 0.5, "Value": -1.0}
            ]
            """;

        var config = await _manager.UpdateRandomAdditiveWalkConfigAsync("AAPL", walkStepsJson, TestContext.Current.CancellationToken);

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
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => _manager.UpdateRandomAdditiveWalkConfigAsync("AAPL", "not valid json", TestContext.Current.CancellationToken));

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
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => _manager.UpdateRandomAdditiveWalkConfigAsync("AAPL", "[]", TestContext.Current.CancellationToken));

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
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var walkStepsJson = """
            [
                {"Probability": 0.6, "Value": 1.0},
                {"Probability": 0.6, "Value": -1.0}
            ]
            """;

        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => _manager.UpdateRandomAdditiveWalkConfigAsync("AAPL", walkStepsJson, TestContext.Current.CancellationToken));

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
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        ModelConfigurationChangedEventArgs? eventArgs = null;
        _manager.ConfigurationChanged += (sender, args) => eventArgs = args;

        var walkStepsJson = """
            [
                {"Probability": 0.5, "Value": 1.0},
                {"Probability": 0.5, "Value": -1.0}
            ]
            """;

        await _manager.UpdateRandomAdditiveWalkConfigAsync("AAPL", walkStepsJson, TestContext.Current.CancellationToken);

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
        await scopedContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        instrument.ModelType = null!;

        var changed = await _manager.EnsureModelTypeAsync(instrument, scopedContext, TestContext.Current.CancellationToken);

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
        await scopedContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        instrument.ModelType = "";

        var changed = await _manager.EnsureModelTypeAsync(instrument, scopedContext, TestContext.Current.CancellationToken);

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
        await scopedContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var changed = await _manager.EnsureModelTypeAsync(instrument, scopedContext, TestContext.Current.CancellationToken);

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
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var instruments = await _manager.LoadAndInitializeAllInstrumentsAsync(TestContext.Current.CancellationToken);

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
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await _manager.UpdateTickIntervalAsync("AAPL", 2500, TestContext.Current.CancellationToken);

        Assert.Equal(2500, result);

        //reload
        var instruments = await _manager.LoadAndInitializeAllInstrumentsAsync(TestContext.Current.CancellationToken);
        Assert.True(instruments.ContainsKey("AAPL"));
        Assert.NotNull(instruments["AAPL"]);
        Assert.Equal(2500, instruments["AAPL"].TickIntervalMillieconds);
    }

    [Fact]
    public async Task UpdateTickIntervalAsync_WithNonExistentInstrument_ThrowsInvalidOperationException()
    {
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _manager.UpdateTickIntervalAsync("NONEXISTENT", 1000, TestContext.Current.CancellationToken));

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
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => _manager.UpdateTickIntervalAsync("AAPL", -500, TestContext.Current.CancellationToken));

        Assert.Contains("Tick interval must be a positive integer", ex.Message);
    }

    [Fact]
    public async Task EnsureModelConfigurationAsync_WithRandomMultiplicativeModel_CreatesDefaultConfig()
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
        await scopedContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        await _manager.EnsureModelConfigurationAsync(instrument, scopedContext, TestContext.Current.CancellationToken);

        await scopedContext.Entry(instrument).ReloadAsync(TestContext.Current.CancellationToken);
        await scopedContext.Entry(instrument)
            .Reference(i => i.RandomMultiplicativeConfig)
            .LoadAsync(TestContext.Current.CancellationToken);

        Assert.NotNull(instrument.RandomMultiplicativeConfig);
        Assert.Equal(instrument.Id, instrument.RandomMultiplicativeConfig.InstrumentId);
        Assert.True(instrument.RandomMultiplicativeConfig.StandardDeviation > 0);
    }

    [Fact]
    public async Task EnsureModelConfigurationAsync_WithMeanRevertingModel_CreatesDefaultConfig()
    {
        using var scope = _serviceProvider.CreateScope();
        var scopedContext = scope.ServiceProvider.GetRequiredService<MarketDataContext>();

        var instrument = new Instrument
        {
            Name = "AAPL",
            ModelType = "MeanReverting",
            TickIntervalMillieconds = 1000
        };
        scopedContext.Instruments.Add(instrument);
        await scopedContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        await _manager.EnsureModelConfigurationAsync(instrument, scopedContext, TestContext.Current.CancellationToken);

        await scopedContext.Entry(instrument).ReloadAsync(TestContext.Current.CancellationToken);
        await scopedContext.Entry(instrument)
            .Reference(i => i.MeanRevertingConfig)
            .LoadAsync(TestContext.Current.CancellationToken);

        Assert.NotNull(instrument.MeanRevertingConfig);
        Assert.Equal(instrument.Id, instrument.MeanRevertingConfig.InstrumentId);
        Assert.True(instrument.MeanRevertingConfig.Kappa > 0);
        Assert.True(instrument.MeanRevertingConfig.Mean > 0);
    }

    [Fact]
    public async Task EnsureModelConfigurationAsync_WithFlatModel_CreatesDefaultConfig()
    {
        using var scope = _serviceProvider.CreateScope();
        var scopedContext = scope.ServiceProvider.GetRequiredService<MarketDataContext>();

        var instrument = new Instrument
        {
            Name = "AAPL",
            ModelType = "Flat",
            TickIntervalMillieconds = 1000
        };
        scopedContext.Instruments.Add(instrument);
        await scopedContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        await _manager.EnsureModelConfigurationAsync(instrument, scopedContext, TestContext.Current.CancellationToken);

        await scopedContext.Entry(instrument).ReloadAsync(TestContext.Current.CancellationToken);
        await scopedContext.Entry(instrument)
            .Reference(i => i.FlatConfig)
            .LoadAsync(TestContext.Current.CancellationToken);

        Assert.NotNull(instrument.FlatConfig);
        Assert.Equal(instrument.Id, instrument.FlatConfig.InstrumentId);
    }

    [Fact]
    public async Task EnsureModelConfigurationAsync_WithRandomAdditiveWalkModel_CreatesDefaultConfig()
    {
        using var scope = _serviceProvider.CreateScope();
        var scopedContext = scope.ServiceProvider.GetRequiredService<MarketDataContext>();

        var instrument = new Instrument
        {
            Name = "AAPL",
            ModelType = "RandomAdditiveWalk",
            TickIntervalMillieconds = 1000
        };
        scopedContext.Instruments.Add(instrument);
        await scopedContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        await _manager.EnsureModelConfigurationAsync(instrument, scopedContext, TestContext.Current.CancellationToken);

        await scopedContext.Entry(instrument).ReloadAsync(TestContext.Current.CancellationToken);
        await scopedContext.Entry(instrument)
            .Reference(i => i.RandomAdditiveWalkConfig)
            .LoadAsync(TestContext.Current.CancellationToken);

        Assert.NotNull(instrument.RandomAdditiveWalkConfig);
        Assert.Equal(instrument.Id, instrument.RandomAdditiveWalkConfig.InstrumentId);
        Assert.NotNull(instrument.RandomAdditiveWalkConfig.WalkStepsJson);
    }

    [Fact]
    public async Task EnsureModelConfigurationAsync_WithExistingConfig_DoesNotCreateNew()
    {
        using var scope = _serviceProvider.CreateScope();
        var scopedContext = scope.ServiceProvider.GetRequiredService<MarketDataContext>();

        var config = new RandomMultiplicativeConfig
        {
            StandardDeviation = 0.05,
            Mean = 0.001
        };

        var instrument = new Instrument
        {
            Name = "AAPL",
            ModelType = "RandomMultiplicative",
            TickIntervalMillieconds = 1000,
            RandomMultiplicativeConfig = config
        };
        scopedContext.Instruments.Add(instrument);
        await scopedContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var originalConfigId = config.Id;

        await _manager.EnsureModelConfigurationAsync(instrument, scopedContext, TestContext.Current.CancellationToken);

        await scopedContext.Entry(instrument)
            .Reference(i => i.RandomMultiplicativeConfig)
            .LoadAsync(TestContext.Current.CancellationToken);

        Assert.NotNull(instrument.RandomMultiplicativeConfig);
        Assert.Equal(originalConfigId, instrument.RandomMultiplicativeConfig.Id);
        Assert.Equal(0.05, instrument.RandomMultiplicativeConfig.StandardDeviation);
        Assert.Equal(0.001, instrument.RandomMultiplicativeConfig.Mean);
    }

    [Fact]
    public async Task EnsureModelConfigurationAsync_WithMeanRevertingAndExistingPrice_UsesLastPriceAsMean()
    {
        using var scope = _serviceProvider.CreateScope();
        var scopedContext = scope.ServiceProvider.GetRequiredService<MarketDataContext>();

        var instrument = new Instrument
        {
            Name = "AAPL",
            ModelType = "MeanReverting",
            TickIntervalMillieconds = 1000
        };
        scopedContext.Instruments.Add(instrument);
        await scopedContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        scopedContext.Prices.Add(new Price
        {
            Instrument = "AAPL",
            Value = 175.50m,
            Timestamp = DateTime.UtcNow
        });
        await scopedContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        await _manager.EnsureModelConfigurationAsync(instrument, scopedContext, TestContext.Current.CancellationToken);

        await scopedContext.Entry(instrument)
            .Reference(i => i.MeanRevertingConfig)
            .LoadAsync(TestContext.Current.CancellationToken);

        Assert.NotNull(instrument.MeanRevertingConfig);
        Assert.Equal(175.50, instrument.MeanRevertingConfig.Mean);
    }

    [Fact]
    public async Task GetOrCreateInstrumentAsync_WithNewInstrument_CreatesInstrument()
    {
        var (instrument, created) = await _manager.GetOrCreateInstrumentAsync("TSLA", 1000, 250.0m, DateTime.UtcNow, ct: TestContext.Current.CancellationToken);

        Assert.True(created);
        Assert.NotNull(instrument);
        Assert.Equal("TSLA", instrument.Name);
        Assert.Equal(1000, instrument.TickIntervalMillieconds);
        Assert.Equal("Flat", instrument.ModelType);
    }

    [Fact]
    public async Task GetOrCreateInstrumentAsync_WithNewInstrument_CreatesDefaultConfiguration()
    {
        var result = await _manager.GetOrCreateInstrumentAsync("TSLA", 1000, 250.0m, DateTime.UtcNow, ct: TestContext.Current.CancellationToken);

        using var scope = _serviceProvider.CreateScope();
        var scopedContext = scope.ServiceProvider.GetRequiredService<MarketDataContext>();

        var instrument = await scopedContext.Instruments
            .Include(i => i.FlatConfig)
            .FirstOrDefaultAsync(i => i.Name == "TSLA", TestContext.Current.CancellationToken);

        Assert.NotNull(instrument);
        Assert.NotNull(instrument.FlatConfig);
    }

    [Fact]
    public async Task GetOrCreateInstrumentAsync_WithExistingInstrument_ReturnsExisting()
    {
        _context.Instruments.Add(new Instrument
        {
            Name = "AAPL",
            ModelType = "RandomMultiplicative",
            TickIntervalMillieconds = 1500
        });
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var (instrument, created) = await _manager.GetOrCreateInstrumentAsync("AAPL", 1000, 150.0m, DateTime.UtcNow, ct: TestContext.Current.CancellationToken);

        Assert.False(created);
        Assert.NotNull(instrument);
        Assert.Equal("AAPL", instrument.Name);
        Assert.Equal(1500, instrument.TickIntervalMillieconds);
        Assert.Equal("RandomMultiplicative", instrument.ModelType);
    }

    [Fact]
    public async Task GetOrCreateInstrumentAsync_WithNewInstrument_RaisesEvent()
    {
        ModelConfigurationChangedEventArgs? eventArgs = null;
        _manager.InstrumentAdded += (_, args) => eventArgs = args;

        await _manager.GetOrCreateInstrumentAsync("NVDA", 1000, 500.0m, DateTime.UtcNow, ct: TestContext.Current.CancellationToken);

        Assert.NotNull(eventArgs);
        Assert.Equal("NVDA", eventArgs.InstrumentName);
    }

    [Fact]
    public async Task GetOrCreateInstrumentAsync_WithExistingInstrument_DoesNotRaiseEvent()
    {
        _context.Instruments.Add(new Instrument
        {
            Name = "AAPL",
            ModelType = "Flat",
            TickIntervalMillieconds = 1000
        });
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        ModelConfigurationChangedEventArgs? eventArgs = null;
        _manager.InstrumentAdded += (_, args) => eventArgs = args;

        var (instrument, _) = await _manager.GetOrCreateInstrumentAsync("AAPL", 1000, 150.0m, DateTime.UtcNow, ct: TestContext.Current.CancellationToken);

        Assert.Null(eventArgs);
        Assert.Equal("AAPL", instrument.Name);
    }

    [Fact]
    public async Task GetOrCreateInstrumentAsync_WithCustomModelType_CreatesWithSpecifiedModel()
    {
        var (instrument, created) = await _manager.GetOrCreateInstrumentAsync("AMZN", 2000, 180.0m, DateTime.UtcNow, "RandomMultiplicative", TestContext.Current.CancellationToken);

        Assert.True(created);
        Assert.NotNull(instrument);
        Assert.Equal("AMZN", instrument.Name);
        Assert.Equal("RandomMultiplicative", instrument.ModelType);
    }

    [Fact]
    public async Task GetOrCreateInstrumentAsync_LoadsConfigurations()
    {
        var result1 = await _manager.GetOrCreateInstrumentAsync("META", 1000, 350.0m, DateTime.UtcNow, ct: TestContext.Current.CancellationToken);
        var result2 = await _manager.GetOrCreateInstrumentAsync("AMZN", 1000, 350.0m, DateTime.UtcNow, "RandomMultiplicative", TestContext.Current.CancellationToken);

        Assert.True(result1.created);
        Assert.NotNull(result1.instrument);
        Assert.True(result2.created);
        Assert.NotNull(result2.instrument);

        using var scope = _serviceProvider.CreateScope();
        var scopedContext = scope.ServiceProvider.GetRequiredService<MarketDataContext>();

        var meta = await scopedContext.Instruments
            .Include(i => i.RandomMultiplicativeConfig)
            .Include(i => i.MeanRevertingConfig)
            .Include(i => i.FlatConfig)
            .Include(i => i.RandomAdditiveWalkConfig)
            .FirstOrDefaultAsync(i => i.Name == "META", TestContext.Current.CancellationToken);
        var amzn = await scopedContext.Instruments
            .Include(i => i.RandomMultiplicativeConfig)
            .Include(i => i.MeanRevertingConfig)
            .Include(i => i.FlatConfig)
            .Include(i => i.RandomAdditiveWalkConfig)
            .FirstOrDefaultAsync(i => i.Name == "AMZN", TestContext.Current.CancellationToken);

        Assert.NotNull(meta);
        Assert.NotNull(meta.FlatConfig);
        Assert.Null(meta.RandomMultiplicativeConfig);
        Assert.Null(meta.MeanRevertingConfig);
        Assert.Null(meta.RandomAdditiveWalkConfig);

        Assert.NotNull(amzn);
        Assert.NotNull(amzn.RandomMultiplicativeConfig);
        Assert.Null(amzn.MeanRevertingConfig);
        Assert.Null(amzn.FlatConfig);
        Assert.Null(amzn.RandomAdditiveWalkConfig);
    }

    [Fact]
    public async Task GetOrCreateInstrumentAsync_WithInvalidModelType_DefaultsToFlat()
    {
        var (instrument, created) = await _manager.GetOrCreateInstrumentAsync("NFLX", 1000, 300.0m, DateTime.UtcNow, "InvalidModel", TestContext.Current.CancellationToken);
        Assert.True(created);
        Assert.NotNull(instrument);
        Assert.Equal("NFLX", instrument.Name);
        Assert.Equal("Flat", instrument.ModelType);
        Assert.NotNull(instrument.FlatConfig);
        Assert.Null(instrument.RandomMultiplicativeConfig);
        Assert.Null(instrument.MeanRevertingConfig);
        Assert.Null(instrument.RandomAdditiveWalkConfig);
    }

    [Fact]
    public async Task GetInstrumentWithConfigurationsAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        _context.Instruments.Add(new Instrument
        {
            Name = "AAPL",
            ModelType = "Flat",
            TickIntervalMillieconds = 1000
        });
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _manager.GetInstrumentWithConfigurationsAsync("AAPL", cts.Token));
    }

    [Fact]
    public async Task UpdateRandomMultiplicativeConfigAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        _context.Instruments.Add(new Instrument
        {
            Name = "AAPL",
            ModelType = "RandomMultiplicative",
            TickIntervalMillieconds = 1000
        });
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _manager.UpdateRandomMultiplicativeConfigAsync("AAPL", 0.03, 0.0002, cts.Token));
    }

    public void Dispose()
    {
        _context.Dispose();
        _serviceProvider.Dispose();
    }
}
