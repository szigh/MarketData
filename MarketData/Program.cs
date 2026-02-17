using Microsoft.EntityFrameworkCore;
using MarketData.Data;
using MarketData.Services;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<MarketDataContext>(options =>
    options.UseSqlite("Data Source=MarketData.db"));

builder.Services.Configure<MarketDataGeneratorOptions>(
    builder.Configuration.GetSection(MarketDataGeneratorOptions.SectionName));

builder.Services.AddSingleton<IInstrumentModelManager, InstrumentModelManager>();

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