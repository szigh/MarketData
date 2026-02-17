using MarketData.Grpc;
using MarketData.Wpf.Client.Services;
using MarketData.Wpf.Shared;
using System;

namespace MarketData.Wpf.Client.ViewModels;

public class ModelConfigViewModel : ViewModelBase
{
    private readonly string _instrument;
    private readonly ModelConfigService _modelService;

    private string _modelTypeName;

    public ModelConfigViewModel(string instrument, 
        ModelConfigService modelService,
        ConfigurationsResponse configurations)
    {
        _instrument = instrument;
        _modelService = modelService;
        _modelTypeName = configurations.ActiveModel;
    }

    public string Instrument
    {
        get => _instrument;
    }

    public string ModelTypeName 
    { 
        get => _modelTypeName;
        set => SetProperty(ref _modelTypeName, value);
    }
}
