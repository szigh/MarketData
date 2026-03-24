using MarketData.Wpf.Client.ViewModels.ModelConfigs;

namespace MarketData.Client.Wpf.ViewModels.AddInstrument.Steps;

public class ConfigureModelParameters : AddInstrumentViewModelBase
{
    private ModelConfigParamsViewModelBase? _modelConfig;

    public ConfigureModelParameters(ModelConfigParamsViewModelBase? modelConfigViewModel) : base()
    {
        _modelConfig = modelConfigViewModel;
    }

    public ModelConfigParamsViewModelBase? ModelConfig
    {
        get => _modelConfig;
        set => SetProperty(ref _modelConfig, value);
    }

    protected override void UpdateValidationErrors()
    {
        ClearAllErrors();

        if (ModelConfig == null)
        {
            AddError(nameof(ModelConfig), "Model configuration is required.");
        }
        else if (!ModelConfig.ValidateProperties())
        {
            string errorMessage;
            if (ModelConfig is RandomAdditiveWalkConfigViewModel randomAdditiveWalkConfig)
            {
                errorMessage = randomAdditiveWalkConfig.ValidationMessage;
            }
            else
            {
                errorMessage = "Model configuration is invalid.";
            }
            AddError(nameof(ModelConfig), errorMessage);
        }
    }
}
