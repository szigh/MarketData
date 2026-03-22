using MarketData.Wpf.Client.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Serilog;
using System.Windows;

namespace MarketData.Wpf.Client;

public partial class App : Application
{
    private IServiceProvider? _serviceProvider;
    private IConfiguration? _configuration;
    private TracerProvider? _tracerProvider;
    private MeterProvider? _meterProvider;

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
            Bootstrapper.LogBanner();
            Log.Information($"");
            Log.Information("Starting WPF Market Data Client");

            var services = new ServiceCollection();

            services.AddSingleton<IConfiguration>(_configuration);

            services.AddLogging(builder =>
            {
                builder.ClearProviders();
                builder.AddSerilog(dispose: true);
                builder.AddOpenTelemetryLogging(_configuration);
            });

            services.AddOpenTelemetry(_configuration);

            services.ConfigureServices();

            _serviceProvider = services.BuildServiceProvider();

            // Capture OpenTelemetry providers for explicit disposal/flushing
            _tracerProvider = _serviceProvider.GetService<TracerProvider>();
            _meterProvider = _serviceProvider.GetService<MeterProvider>();

            Bootstrapper.InitializeGrpcConnections(_serviceProvider);

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

    protected override void OnExit(ExitEventArgs e)
    {
        Log.Information("Shutting down WPF Market Data Client");

        // Force flush OpenTelemetry providers before disposing
        try
        {
            _tracerProvider?.ForceFlush();
            _meterProvider?.ForceFlush();
            Log.Information("OpenTelemetry providers flushed");
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Error flushing OpenTelemetry providers");
        }

        if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }

        _tracerProvider?.Dispose();
        _meterProvider?.Dispose();

        Log.CloseAndFlush();
        base.OnExit(e);
    }
}

