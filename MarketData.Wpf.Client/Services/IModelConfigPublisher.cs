using MarketData.Grpc;
using MarketData.Wpf.Client.ViewModels.ModelConfigs;

namespace MarketData.Wpf.Client.Services
{
    public interface IModelConfigPublisher
    {
        void Dispose();
        void LogPublishResultsSummary();
        Task<bool> PublishTickInterval(int tickIntervalMs, CancellationToken ct = default);
        Task<bool> TryPublishModelParams(ModelConfigParamsViewModelBase activeConfigVm, CancellationToken ct = default);
        Task<(bool success, ConfigurationsResponse? configs)> TrySwitchModel(CancellationToken ct = default);
    }
}