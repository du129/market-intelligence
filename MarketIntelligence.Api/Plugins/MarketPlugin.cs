using System.ComponentModel;
using System.Text;
using Microsoft.SemanticKernel;
using MarketIntelligence.Shared;
using MarketIntelligence.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace MarketIntelligence.Api.Plugins;

public class MarketPlugin
{
    [KernelFunction, Description("Gets recent stock price history.")]
    public string GetStockHistory([Description("Stock symbol (e.g. MSFT, SPY)")] string symbol)
    {
        Console.WriteLine($"[Tool] SQL SELECT for {symbol}...");

        using var db = new MarketContext();
        
        // Fetch last 7 days
        var history = db.Stocks
            .Where(s => s.Symbol == symbol)
            .OrderByDescending(s => s.Date)
            .Take(7)
            .ToList();

        if (!history.Any()) 
        {
            Console.WriteLine($"[Tool] No data found for {symbol}");
            return $"I queried the SQL database for {symbol} but found no records. The Ingestor might not be tracking this ticker.";
        }

        var sb = new StringBuilder($"### Price History for {symbol}:\n");
        sb.AppendLine("| Date | Close Price |");
        sb.AppendLine("|---|---|");
        foreach (var day in history)
        {
            sb.AppendLine($"| {day.Date:yyyy-MM-dd} | ${day.Close:F2} |");
        }
        return sb.ToString();
    }

    [KernelFunction, Description("Generates chart data for stock comparison.")]
    public ChartResponse GetChartData(
        [Description("First stock symbol")] string symbol1,
        [Description("Second stock symbol")] string symbol2)
    {
        Console.WriteLine($"[Tool] Chart Generation for {symbol1} vs {symbol2}...");

        var data1 = GetHistoryList(symbol1);
        var data2 = GetHistoryList(symbol2);

        if (!data1.Any() && !data2.Any())
        {
            return new ChartResponse { Title = "No Data Found in Database" };
        }

        var chartData = new ChartResponse
        {
            Title = $"{symbol1} vs {symbol2} Performance",
            Series1Name = symbol1,
            Series2Name = symbol2
        };

        // Align data (simplified)
        foreach (var d1 in data1)
        {
            // Find matching date in d2
            var d2 = data2.FirstOrDefault(d => d.Date.Date == d1.Date.Date);
            
            chartData.Data.Add(new ChartPoint
            {
                Label = d1.Date.ToString("MM/dd"),
                Value1 = d1.Close,
                Value2 = d2?.Close ?? 0
            });
        }
        // Reverse so chart goes left-to-right (Oldest to Newest)
        chartData.Data.Reverse(); 

        return chartData;
    }

    [KernelFunction, Description("Retrieves the latest 'Alpha Hunter' investment report.")]
    public string GetDailyAlphaReport()
    {
        Console.WriteLine("[Tool] Generating Daily Alpha Report...");

        using var db = new MarketContext();
        
        // Get Recommendations from the last 48 hours
        var recs = db.Recommendations
            .Where(r => r.DateGenerated > DateTime.UtcNow.AddHours(-48))
            .OrderBy(r => r.MatchingTheme)
            .ToList();

        if (!recs.Any()) return "The Alpha Hunter database contains no recommendations from the last 48 hours.";

        var sb = new StringBuilder("## 🦅 Daily Alpha Hunter Report\n\n");
        
        var grouped = recs.GroupBy(r => r.MatchingTheme);

        foreach (var group in grouped)
        {
            sb.AppendLine($"### Theme: {group.Key}");
            foreach (var item in group)
            {
                sb.AppendLine($"- **{item.Symbol}** (${item.Price:F2}): {item.AI_Reasoning}");
            }
            sb.AppendLine("");
        }

        return sb.ToString();
    }
    
    [KernelFunction, Description("Gets macro-economic themes from banks.")]
    public string GetMacroInsights()
    {
        using var db = new MarketContext();
        var themes = db.MarketThemes.OrderByDescending(t => t.RecordedDate).Take(5).ToList();
        
        if(!themes.Any()) return "No macro themes found in database.";
        
        var sb = new StringBuilder();
        foreach(var t in themes)
        {
            sb.AppendLine($"- **{t.Source}**: {t.Sentiment} on '{t.ThemeTitle}'");
        }
        return sb.ToString();
    }

    private List<StockData> GetHistoryList(string symbol)
    {
        using var db = new MarketContext();
        return db.Stocks
            .Where(s => s.Symbol == symbol)
            .OrderByDescending(s => s.Date)
            .Take(10)
            .ToList();
    }
}