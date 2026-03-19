using Grpc.Net.ClientFactory;
using MarketData.Client.Shared.Configuration;
using MarketData.Client.Wpf.Services;
using MarketData.Grpc;
using MarketData.Wpf.Client.Services;
using MarketData.Wpf.Client.ViewModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;
using System.Windows;

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

        // Configure Serilog
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(_configuration)
            .Filter.ByExcluding(logEvent => logEvent.Properties.ContainsKey("InitializingCandleChart"))
            .CreateLogger();

        try
        {
            LogBanner();
            Log.Information($"");
            Log.Information("Starting WPF Market Data Client");

            var services = new ServiceCollection();

            services.AddSingleton<IConfiguration>(_configuration);

            services.AddLogging(builder =>
            {
                builder.ClearProviders();
                builder.AddSerilog(dispose: true);
            });

            services.ConfigureServices();

            _serviceProvider = services.BuildServiceProvider();

            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();

            Log.Information("WPF Market Data Client started successfully");
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
            throw;
        }
    }

    private static void LogBanner()
    {
        const string banner =
@"
__  __            _        _         _       _        
 |  \/  |          | |      | |       | |     | |       
 | \  / | __ _ _ __| | _____| |_    __| | __ _| |_ __ _ 
 | |\/| |/ _` | '__| |/ / _ \ __|  / _` |/ _` | __/ _` |
 | |  | | (_| | |  |   <  __/ |_  | (_| | (_| | || (_| |
 |_|  |_|\__,_|_|  |_|\_\___|\__|  \__,_|\__,_|\__\__,_|
 __          _______  ______        _ _            _    
 \ \        / /  __ \|  ____|      | (_)          | |   
  \ \  /\  / /| |__) | |__      ___| |_  ___ _ __ | |_  
   \ \/  \/ / |  ___/|  __|    / __| | |/ _ \ '_ \| __| 
    \  /\  /  | |    | |      | (__| | |  __/ | | | |_  
     \/  \/   |_|    |_|       \___|_|_|\___|_| |_|\__| 
                                                        
                                                        ";
        Log.Logger.Information(banner);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Log.Information("Shutting down WPF Market Data Client");

        if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }

        Log.CloseAndFlush();
        base.OnExit(e);
    }
}

internal static class ServiceCollectionExtensions
{
    internal static IServiceCollection ConfigureServices(this IServiceCollection services)
    {
        Log.Logger.Information("Configuring services and options");

        services.AddOptions<GrpcSettings>()
            .BindConfiguration(GrpcSettings.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<CandleChartSettings>()
            .BindConfiguration(CandleChartSettings.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.ConfigureGrpcClients();

        services.AddSingleton<IModelConfigService, ModelConfigService>();
        services.AddSingleton<IDialogService, DialogService>();
        services.AddTransient<InstrumentViewModelFactory>();

        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<MainWindow>();

        return services;
    }

    internal static IServiceCollection ConfigureGrpcClients(this IServiceCollection services)
    {
        Log.Logger.Information("Configuring gRPC clients with server URL from configuration");

        services.AddGrpcClient<MarketDataService.MarketDataServiceClient>(ConfigureClient);
        services.AddGrpcClient<ModelConfigurationService.ModelConfigurationServiceClient>(ConfigureClient);

        return services;

        static void ConfigureClient(IServiceProvider sp, GrpcClientFactoryOptions options) =>
            options.Address = new Uri(sp.GetRequiredService<IOptions<GrpcSettings>>().Value.ServerUrl);
    }
}

