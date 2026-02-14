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
builder.Services.AddSingleton<IPriceSimulator, RandomMultiplicativeProcess>(_ =>
{
    //TODO make standard deviation configurable

    // 99% of moves stay within 1% of current price
    // 99% ≈ ±2.576 standard deviations
    // Therefore: σ = 0.01 / 2.576 ≈ 0.00388
    var standardDeviation = 0.00388;

    return new RandomMultiplicativeProcess(standardDeviation);
});
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
