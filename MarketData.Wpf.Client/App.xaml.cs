using System.Windows;
using MarketData.Client.Shared.Configuration;
using MarketData.Grpc;
using MarketData.Wpf.Client.Services;
using MarketData.Wpf.Client.ViewModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace MarketData.Wpf.Client;

public partial class App : Application
{
    private IServiceProvider? _serviceProvider;
    private IConfiguration? _configuration;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();

        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        services.Configure<GrpcSettings>(_configuration!.GetSection(GrpcSettings.SectionName));

        services.AddGrpcClient<MarketDataService.MarketDataServiceClient>(
            (serviceProvider, options) =>
        {
            var grpcSettings = serviceProvider.GetRequiredService<IOptions<GrpcSettings>>().Value;
            options.Address = new Uri(grpcSettings.ServerUrl);
        });

        services.AddSingleton<IModelConfigService, ModelConfigService>();
        services.AddTransient<InstrumentViewModelFactory>();

        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<MainWindow>();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
        base.OnExit(e);
    }
}
