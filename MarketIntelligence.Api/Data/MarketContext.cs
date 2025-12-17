using Microsoft.EntityFrameworkCore;
using MarketIntelligence.Shared;

namespace MarketIntelligence.Api.Data;

public class MarketContext : DbContext
{
    public DbSet<StockData> Stocks { get; set; }
    public DbSet<MarketTheme> MarketThemes { get; set; }
    public DbSet<StockRecommendation> Recommendations { get; set; }
    public DbSet<EtfRecommendation> EtfRecommendations { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
{
    // 1. Check if a full connection string is provided (For Azure SQL)
    var connectionString = Environment.GetEnvironmentVariable("SQL_CONNECTION_STRING");

    // 2. Fallback to Docker logic if empty (For Local Dev)
    if (string.IsNullOrEmpty(connectionString))
    {
        var dbHost = Environment.GetEnvironmentVariable("DB_HOST") ?? "localhost";
        connectionString = $"Server={dbHost},1433;Database=MarketDb;User Id=sa;Password=Password123!;TrustServerCertificate=True;";
    }

    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5, 
            maxRetryDelay: TimeSpan.FromSeconds(30), 
            errorNumbersToAdd: null);
    });
}
}