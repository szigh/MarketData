using MarketData.Client.Wpf.Services;
using MarketData.Client.Wpf.ViewModels;
using MarketData.Client.Wpf.ViewModels.AddInstrument;
using MarketData.Client.Wpf.ViewModels.ModelConfigs;
using MarketData.Grpc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;

namespace MarketData.Client.Wpf.Bootstrapper;

#region Delegate definitions for ViewModel factories

public delegate InstrumentViewModel CreateInstrumentViewModel(string instrumentName);
public delegate ModelConfigViewModel CreateModelConfigViewModel(string instrumentName, ConfigurationsResponse config, IEnumerable<string> availableModels);
public delegate ModelConfigParamsViewModelBase? CreateModelConfigParamsViewModel(ConfigurationsResponse config);
public delegate InstrumentTabViewModel CreateInstrumentTabViewModel(InstrumentViewModel instrumentViewModel);
public delegate AddInstrumentWizardViewModel CreateAddInstrumentWizardViewModel();

#endregion

internal static class ViewModelConstructors
{
    private static readonly Serilog.ILogger Logger = Log.ForContext(typeof(Bootstrapper));

    internal static AddInstrumentWizardViewModel CreateAddInstrumentViewModel(this IServiceProvider sp)
    {
        return new AddInstrumentWizardViewModel(
                        sp.GetRequiredService<IModelConfigService>(),
                        sp.GetRequiredService<CreateModelConfigParamsViewModel>(),
                        sp.GetRequiredService<ILogger<AddInstrumentWizardViewModel>>(),
                        sp.GetRequiredService<IDialogService>());
    }

    internal static InstrumentViewModel CreateInstrumentViewModel(this IServiceProvider sp, string instrumentName)
    {
        return new InstrumentViewModel(
                        instrumentName,
                        sp.GetRequiredService<MarketDataService.MarketDataServiceClient>(),
                        sp.GetRequiredService<IModelConfigService>(),
                        sp.GetRequiredService<IOptions<CandleChartSettings>>(),
                        sp.GetRequiredService<CreateModelConfigViewModel>(),
                        sp.GetRequiredService<IDialogService>(),
                        sp.GetRequiredService<ILogger<InstrumentViewModel>>());
    }

    internal static ModelConfigViewModel CreateModelConfigViewModel(this IServiceProvider sp,
        string instrumentName, ConfigurationsResponse config, IEnumerable<string> availableModels)
    {
        return new ModelConfigViewModel(
                        instrumentName,
                        config,
                        availableModels,
                        sp.GetRequiredService<IModelConfigService>(),
                        sp.GetRequiredService<CreateModelConfigParamsViewModel>(),
                        sp.GetRequiredService<IDialogService>(),
                        sp.GetRequiredService<ILogger<ModelConfigViewModel>>());
    }

    internal static ModelConfigParamsViewModelBase? CreateModelConfigParamsViewModel(this IServiceProvider sp,
        ConfigurationsResponse config)
    {
        var modelType = config.ActiveModel;
        var instrumentName = config.InstrumentName;

        Logger.Information("Creating model config view model for instrument {InstrumentName} " +
            "with model type {ModelType}", instrumentName, modelType);

        var modelConfigService = sp.GetRequiredService<IModelConfigService>();
        var dialogService = sp.GetRequiredService<IDialogService>();

        return modelType switch
        {
            "RandomMultiplicative" when config.RandomMultiplicative != null =>
                new RandomMultiplicativeConfigViewModel(instrumentName, config.RandomMultiplicative,
                modelConfigService, dialogService, sp.GetRequiredService<ILogger<RandomMultiplicativeConfigViewModel>>()),

            "MeanReverting" when config.MeanReverting != null =>
                new MeanRevertingConfigViewModel(instrumentName, config.MeanReverting,
                modelConfigService, dialogService, sp.GetRequiredService<ILogger<MeanRevertingConfigViewModel>>()),
            "Flat" =>
                new FlatConfigViewModel(instrumentName,
                dialogService, sp.GetRequiredService<ILogger<FlatConfigViewModel>>()),

            "RandomAdditiveWalk" when config.RandomAdditiveWalk != null =>
                new RandomAdditiveWalkConfigViewModel(instrumentName, config.RandomAdditiveWalk,
                modelConfigService, dialogService, sp.GetRequiredService<ILogger<RandomAdditiveWalkConfigViewModel>>()),

            _ => null
        };
    }

    internal static InstrumentTabViewModel CreateInstrumentTabViewModel(this IServiceProvider _,
        InstrumentViewModel instrumentVM)
    {
        return new InstrumentTabViewModel(instrumentVM);
    }
}
