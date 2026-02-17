using MarketData.Grpc;
using System.Collections.ObjectModel;

namespace MarketData.Wpf.Client.ViewModels.ModelConfigs;

public class RandomAdditiveWalkConfigViewModel : ModelConfigViewModelBase
{
    private readonly RandomAdditiveWalkConfigData _config;

    public RandomAdditiveWalkConfigViewModel(string instrumentName, RandomAdditiveWalkConfigData config)
        : base(instrumentName)
    {
        _config = config;
        WalkSteps = new ObservableCollection<WalkStepViewModel>(
            _config.WalkSteps.Select(s => new WalkStepViewModel(s.Probability, s.StepValue))
        );
    }

    public ObservableCollection<WalkStepViewModel> WalkSteps { get; }
    public int StepCount => WalkSteps.Count;

    protected override Task<bool> TryExecutePublishConfigChangesAsync()
    {
        throw new NotImplementedException();
    }
}

public class WalkStepViewModel
{
    public WalkStepViewModel(double probability, double stepValue)
    {
        Probability = probability;
        StepValue = stepValue;
    }

    public double Probability { get; }
    public double StepValue { get; }
    public string ProbabilityPercentage => $"{Probability * 100:F1}%";
}
