using MarketData.Models;
using Microsoft.EntityFrameworkCore;

namespace MarketData.Data;

public class MarketDataContext : DbContext
{
    public MarketDataContext(DbContextOptions<MarketDataContext> options)
        : base(options)
    {
    }

    public DbSet<Price> Prices { get; set; }
    public DbSet<Instrument> Instruments { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Instrument>()
            .HasIndex(i => i.Name)
            .IsUnique();
    }
}
