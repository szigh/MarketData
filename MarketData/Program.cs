using Microsoft.EntityFrameworkCore;
using MarketData.Data;
using MarketData.Services;
using Scalar.AspNetCore;
using MarketData.PriceSimulator;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<MarketDataContext>(options =>
    options.UseSqlite("Data Source=MarketData.db"));

builder.Services.Configure<MarketDataGeneratorOptions>(
    builder.Configuration.GetSection(MarketDataGeneratorOptions.SectionName));

AddPriceSimulator(builder);

builder.Services.AddHostedService<MarketDataGeneratorService>();

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddGrpc();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

// Map gRPC service
app.MapGrpcService<MarketDataGrpcService>();

app.Run();

static void AddPriceSimulator(WebApplicationBuilder builder)
{
    if (false)
    {
        //Random Multiplicative Process

        builder.Services.AddSingleton<IPriceSimulator, RandomMultiplicativeProcess>(_ =>
        {
            //TODO make standard deviation configurable

            // 99% of moves stay within 1% of current price
            // 99% ≈ ±2.576 standard deviations
            // Therefore: σ = 0.01 / 2.576 ≈ 0.00388
            var standardDeviation = 0.00388;

            return new RandomMultiplicativeProcess(standardDeviation);
        });
    }
    if (false)
    {
        //flat price simulator for testing
        builder.Services.AddSingleton<IPriceSimulator, Flat>();
    }
    if (false)
    {
        builder.Services.AddSingleton<IPriceSimulator, RandomAdditiveWalk>(_ =>
        {
            var walkSteps = new RandomWalkSteps(
            [
                new(0.25, -0.01 ),
                new(0.25, -0.005),
                new(0.25, 0.005 ),
                new(0.25, 0.01 )
            ]);
            return new RandomAdditiveWalk(walkSteps);
        });
    }
    if (true)
    {
        builder.Services.AddSingleton<IPriceSimulator, MeanRevertingProcess>(_ =>
        {
            const double SECONDS_PER_YEAR = 252 * 6.5 * 3600; // 5,875,200

            return new MeanRevertingProcess(
                //mean: 10_000.0,
                mean: 1600,
                kappa: 100 / SECONDS_PER_YEAR,
                sigma: 0.5,
                dt: 0.1  // 0.1 second time step
            );
        });
    }
}