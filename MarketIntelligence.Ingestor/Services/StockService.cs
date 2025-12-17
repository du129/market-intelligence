using MarketIntelligence.Shared;
using Microsoft.Extensions.Configuration;
using Polly;
using Polly.Retry;
using Newtonsoft.Json.Linq;

namespace MarketIntelligence.Ingestor.Services;

public class StockService
{
    private readonly HttpClient _http;
    private readonly string _apiKey;
    private readonly AsyncRetryPolicy<HttpResponseMessage> _retryPolicy;

    public StockService(IConfiguration config)
    {
        _apiKey = config["AlphaVantageKey"]!;
        _http = new HttpClient { BaseAddress = new Uri("https://www.alphavantage.co/") };

        // REAL WORLD: Retry 3 times if the API is busy (429) or down (500)
        _retryPolicy = Policy
            .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(2));
    }

    public async Task<List<StockData>> GetHistoryAsync(string symbol)
    {
        Console.WriteLine($"[AlphaVantage] Requesting real data for {symbol}...");
        
        // API: Time Series Daily (Compact)
        var url = $"query?function=TIME_SERIES_DAILY&symbol={symbol}&apikey={_apiKey}";
        
        var response = await _retryPolicy.ExecuteAsync(() => _http.GetAsync(url));
        var content = await response.Content.ReadAsStringAsync();

        var json = JObject.Parse(content);
        var timeSeries = json["Time Series (Daily)"];

        if (timeSeries == null) 
        {
            Console.WriteLine($"[Error] API Limit Reached or Symbol Not Found for {symbol}");
            return new List<StockData>();
        }

        var results = new List<StockData>();
        int count = 0;

        foreach (JProperty day in timeSeries)
        {
            if (count++ >= 10) break; // Only take last 10 days

            var date = DateTime.Parse(day.Name);
            var close = decimal.Parse(day.Value["4. close"]!.ToString());
            var volume = long.Parse(day.Value["5. volume"]!.ToString());

            results.Add(new StockData 
            { 
                Symbol = symbol, 
                Date = date, 
                Close = close, 
                Volume = volume 
            });
        }

        return results;
    }
}