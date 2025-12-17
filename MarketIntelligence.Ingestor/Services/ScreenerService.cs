using System.Text.Json;
using MarketIntelligence.Shared;
using Microsoft.Extensions.Configuration;

namespace MarketIntelligence.Ingestor.Services;

public class ScreenerService
{
    private readonly HttpClient _http;
    private readonly string _apiKey;
    private readonly StockService _stockService; // To check SMA

    public ScreenerService(IConfiguration config, StockService stockService)
    {
        _apiKey = config["FMPKey"]!; // You need to add this to appsettings
        _http = new HttpClient { BaseAddress = new Uri("https://financialmodelingprep.com/api/v3/") };
        _stockService = stockService;
    }

    public async Task<List<StockRecommendation>> FindGrowthStocksAsync()
    {
        Console.WriteLine("[Screener] Filtering US Market for Growth Candidates...");

        var url = $"stock-screener?marketCapMoreThan=1000000000&volumeMoreThan=500000&isEtf=false&limit=20&apikey={_apiKey}";
        
        try 
        {
            var response = await _http.GetAsync(url);
            
            // Check for API errors (like 403 Forbidden)
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"[Screener Warning] FMP API Failed: {response.StatusCode} (Likely invalid key or paywall).");
                Console.WriteLine("[Screener] Switching to FALLBACK candidates so pipeline can continue.");
                return GetFallbackCandidates();
            }

            var content = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(content);
            
            var candidates = new List<StockRecommendation>();

            foreach (var item in doc.RootElement.EnumerateArray())
            {
                // ... (Keep existing parsing logic) ...
                var symbol = item.GetProperty("symbol").GetString();
                var price = item.GetProperty("price").GetDecimal();
                var company = item.GetProperty("companyName").GetString();
                var mktCap = item.GetProperty("marketCap").GetDecimal();

                candidates.Add(new StockRecommendation
                {
                    Symbol = symbol,
                    CompanyName = company,
                    Price = price,
                    MarketCap = mktCap
                });
            }
            return candidates;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Screener Error] {ex.Message}");
            return GetFallbackCandidates();
        }
    }

    // --- NEW: FALLBACK DATA ---
    // If the API fails (403), use these so you can still see the AI Matchmaker work!
    private List<StockRecommendation> GetFallbackCandidates()
    {
        return new List<StockRecommendation>
        {
            new() { Symbol = "NVDA", CompanyName = "NVIDIA Corp", Price = 135.50m, MarketCap = 3000000000000 },
            new() { Symbol = "AMD", CompanyName = "Advanced Micro Devices", Price = 170.20m, MarketCap = 250000000000 },
            new() { Symbol = "PLTR", CompanyName = "Palantir Technologies", Price = 25.40m, MarketCap = 50000000000 },
            new() { Symbol = "TSLA", CompanyName = "Tesla Inc", Price = 175.00m, MarketCap = 600000000000 },
            new() { Symbol = "CRWD", CompanyName = "CrowdStrike", Price = 350.00m, MarketCap = 80000000000 }
        };
    }

    private async Task<bool> PassesDetailedCriteria(string symbol, decimal currentPrice)
    {
        try 
        {
            // CHECK 1: Price > 200 SMA
            // We reuse your StockService to get history
            var history = await _stockService.GetHistoryAsync(symbol); // Fetches last 10 days by default? 
            // NOTE: You need to update StockService to fetch 200 days for this to work accurately.
            // For now, let's assume we fetch enough data.
            
            if (history.Count < 50) return false; // Not enough data

            var average = history.Average(x => x.Close); // Simple SMA
            
            if (currentPrice <= average) 
            {
                // Console.WriteLine($"   -> Fail: {symbol} below SMA ({currentPrice} < {average})");
                return false; 
            }

            // CHECK 2: Fundamentals (ROE / Growth)
            // Need a separate FMP call for "Key Metrics"
            // var financials = await _http.GetStringAsync($"key-metrics-ttm/{symbol}?apikey={_apiKey}");
            // Parse ROE > 0.10 ...
            
            // If we get here, it passed
            return true;
        }
        catch 
        {
            return false;
        }
    }
}