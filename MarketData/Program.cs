using Microsoft.EntityFrameworkCore;
using MarketData.Data;
using MarketData.Services;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<MarketDataContext>(options =>
    options.UseSqlite("Data Source=MarketData.db"));

builder.Services.Configure<MarketDataGeneratorOptions>(
    builder.Configuration.GetSection(MarketDataGeneratorOptions.SectionName));

builder.Services.AddSingleton<IPriceSimulatorFactory, PriceSimulatorFactory>();
builder.Services.AddSingleton<IInstrumentModelManager, InstrumentModelManager>();

builder.Services.AddHostedService<MarketDataGeneratorService>();

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddGrpc();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    // Apply pending migrations at startup in development environment
    //!Important: in production this should be in deployment scripts, not in application code
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<MarketDataContext>();
        context.Database.Migrate();
    }

    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

// Map gRPC services
app.MapGrpcService<MarketDataGrpcService>();
app.MapGrpcService<ModelConfigurationGrpcService>();

app.Run();
