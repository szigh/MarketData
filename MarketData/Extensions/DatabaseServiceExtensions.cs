using MarketData.Configuration;
using MarketData.Data;
using Microsoft.EntityFrameworkCore;

namespace MarketData.Extensions;

/// <summary>
/// Extension methods for configuring database services.
/// </summary>
public static class DatabaseServiceExtensions
{
    /// <summary>
    /// Adds the MarketData database context with SQLite provider.
    /// </summary>
    public static WebApplicationBuilder AddMarketDataDatabase(this WebApplicationBuilder builder)
    {
        var dbOptions = builder.Configuration
            .GetSection(DatabaseOptions.SectionName)
            .Get<DatabaseOptions>() ?? new DatabaseOptions();

        builder.Services.AddDbContext<MarketDataContext>(options =>
            options.UseSqlite(dbOptions.ConnectionString));

        return builder;
    }

    /// <summary>
    /// Applies pending EF Core migrations at startup (Development only).
    /// </summary>
    /// <remarks>
    /// Important: In production, migrations should be in deployment scripts, not in application code.
    /// </remarks>
    public static IApplicationBuilder ApplyDatabaseMigrations(this IApplicationBuilder app)
    {
        using (var scope = app.ApplicationServices.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<MarketDataContext>();
            context.Database.Migrate();
        }

        return app;
    }
}
