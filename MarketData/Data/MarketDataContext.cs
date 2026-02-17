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
    public DbSet<RandomMultiplicativeConfig> RandomMultiplicativeConfigs { get; set; }
    public DbSet<MeanRevertingConfig> MeanRevertingConfigs { get; set; }
    public DbSet<FlatConfig> FlatConfigs { get; set; }
    public DbSet<RandomAdditiveWalkConfig> RandomAdditiveWalkConfigs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Instrument>()
            .HasIndex(i => i.Name)
            .IsUnique();

        // Configure one-to-one relationships for model configurations
        modelBuilder.Entity<RandomMultiplicativeConfig>()
            .HasOne(c => c.Instrument)
            .WithOne(i => i.RandomMultiplicativeConfig)
            .HasForeignKey<RandomMultiplicativeConfig>(c => c.InstrumentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<MeanRevertingConfig>()
            .HasOne(c => c.Instrument)
            .WithOne(i => i.MeanRevertingConfig)
            .HasForeignKey<MeanRevertingConfig>(c => c.InstrumentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<FlatConfig>()
            .HasOne(c => c.Instrument)
            .WithOne(i => i.FlatConfig)
            .HasForeignKey<FlatConfig>(c => c.InstrumentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RandomAdditiveWalkConfig>()
            .HasOne(c => c.Instrument)
            .WithOne(i => i.RandomAdditiveWalkConfig)
            .HasForeignKey<RandomAdditiveWalkConfig>(c => c.InstrumentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
