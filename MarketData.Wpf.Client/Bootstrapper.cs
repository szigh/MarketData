using Grpc.Net.Client;
using MarketData.Client.Shared.Configuration;
using MarketData.Client.Shared.Services;
using MarketData.Client.Wpf.Services;
using MarketData.Grpc;
using MarketData.Wpf.Client.Services;
using MarketData.Wpf.Client.ViewModels;
using MarketData.Wpf.Client.ViewModels.ModelConfigs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;

namespace MarketData.Wpf.Client;

internal static class Bootstrapper
{
    private static readonly Serilog.ILogger Logger = Log.ForContext(typeof(Bootstrapper));

    internal static IServiceCollection ConfigureServices(this IServiceCollection services)
    {
        Logger.Information("Configuring services and options");

        services.AddOptions<GrpcSettings>()
            .BindConfiguration(GrpcSettings.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<CandleChartSettings>()
            .BindConfiguration(CandleChartSettings.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.ConfigureGrpcClients();

        Logger.Information("Registering application specific services");
        services.AddSingleton<IModelConfigService, ModelConfigService>();
        services.AddSingleton<IDialogService, DialogService>();
        services.AddTransient<InstrumentViewModelFactory>();
        services.AddTransient<ModelConfigViewModelFactory>();

        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<MainWindow>();

        return services;
    }

    internal static IServiceCollection ConfigureGrpcClients(this IServiceCollection services)
    {
        Logger.Information("Configuring gRPC clients with server URL from configuration");

        services.AddSingleton(sp => GrpcChannel.ForAddress(sp.GetGrpcServerUrl(), DefaultChannelOptions));

        services.AddSingleton<IGrpcConnectionInitializer>(sp =>
            new GrpcConnectionInitializer(
                sp.GetRequiredService<GrpcChannel>(),
                sp.GetRequiredService<ILogger<GrpcConnectionInitializer>>()));

        services.AddSingleton<MarketDataService.MarketDataServiceClient>(sp =>
            new MarketDataService.MarketDataServiceClient(sp.GetRequiredService<GrpcChannel>()));

        services.AddSingleton<ModelConfigurationService.ModelConfigurationServiceClient>(sp =>
            new ModelConfigurationService.ModelConfigurationServiceClient(sp.GetRequiredService<GrpcChannel>()));

        return services;
    }

    internal static string GetGrpcServerUrl(this IServiceProvider sp) => sp.GetRequiredService<IOptions<GrpcSettings>>().Value.ServerUrl;

    internal static void InitializeGrpcConnections(IServiceProvider serviceProvider)
    {
        var dialogService = serviceProvider.GetRequiredService<IDialogService>();
        Logger.Information("Initializing gRPC connection (to avoid race conditions with lazy-initialization)");

        var connectionInitializer = serviceProvider.GetRequiredService<IGrpcConnectionInitializer>();

        // Run on thread pool to avoid sync context deadlock
        var initTask = Task.Run(async () => await connectionInitializer.InitializeAsync());

        if (!initTask.Wait(TimeSpan.FromSeconds(10)))
        {
            Logger.Warning("gRPC connection initialization timed out, continuing anyway");
        }
        else if (initTask.IsFaulted)
        {
            Logger.Error(initTask.Exception, "gRPC connection initialization failed");
            dialogService.ShowError(
                $"Failed to connect to the server. Please ensure the MarketData service is running.\n\n" +
                $"{initTask.Exception?.GetBaseException().Message}",
                "Connection Error");

            throw initTask.Exception?.GetBaseException() ?? new InvalidOperationException("Failed to initialize gRPC connection");
        }
        else
        {
            Logger.Information("gRPC connection ready");
        }
    }


    private static readonly GrpcChannelOptions DefaultChannelOptions = new()
    {
        InitialReconnectBackoff = TimeSpan.FromMilliseconds(100),
        MaxReconnectBackoff = TimeSpan.FromSeconds(1)
    };

    internal static void LogBanner()
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
}
