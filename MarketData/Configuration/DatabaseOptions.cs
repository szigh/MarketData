namespace MarketData.Configuration;

public class DatabaseOptions
{
    public const string SectionName = "Database";

    public string ConnectionString { get; set; } = "Data Source=MarketData.db";
}
