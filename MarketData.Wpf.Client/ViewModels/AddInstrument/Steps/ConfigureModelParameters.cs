using MarketData.Wpf.Client.ViewModels.ModelConfigs;

namespace MarketData.Client.Wpf.ViewModels.AddInstrument.Steps;

public class ConfigureModelParameters : AddInstrumentViewModelBase
{
    private string _validationMessage = "";
    private ModelConfigViewModelBase? _modelConfig;

    public ConfigureModelParameters(ModelConfigViewModelBase? modelConfigViewModel) : base()
    {
        _modelConfig = modelConfigViewModel;
    }

    public ModelConfigViewModelBase? ModelConfig
    {
        get => _modelConfig;
        set => SetProperty(ref _modelConfig, value);
    }

    protected override void UpdateValidationMessage()
    {
        if (ModelConfig == null)
        {
            ValidationMessage = "Model configuration is required.";
        }
        else if (!ModelConfig.ValidateProperties())
        {
            if (ModelConfig is RandomAdditiveWalkConfigViewModel randomAdditiveWalkConfig)
            {
                ValidationMessage = randomAdditiveWalkConfig.ValidationMessage;
            }
            else
            {
                ValidationMessage = "Model configuration is invalid.";
            }
        }
        else
        {
            ValidationMessage = string.Empty;
        }
    }

    public override string ValidationMessage
    {
        get => _validationMessage;
        protected set => SetProperty(ref _validationMessage, value);
    }
}
