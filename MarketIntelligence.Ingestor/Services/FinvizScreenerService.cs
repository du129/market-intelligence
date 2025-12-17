using PuppeteerSharp;
using MarketIntelligence.Shared;

namespace MarketIntelligence.Ingestor.Services;

public class FinvizScreenerService
{
    // FINVIZ FILTER URL:
    // v=111: Overview View
    // f=cap_midover: Market Cap > 2B (Mid+)
    // fa_epsqqq_o20: EPS Growth Q/Q > 20%
    // fa_salesqqq_o20: Sales Growth Q/Q > 20%
    // sh_avgvol_o200: Volume > 200k
    // o=-marketcap: Sort by Market Cap Descending
    private const string SCREENER_URL = "https://finviz.com/screener.ashx?v=111&f=cap_midover,fa_epsqqq_o20,fa_salesqqq_o20,sh_avgvol_o200&ft=4&o=-marketcap";

    public async Task<List<StockRecommendation>> ScrapeGrowthStocksAsync()
    {
        Console.WriteLine("[Screener] Scraping Finviz for high-growth candidates...");

        var launchOptions = new LaunchOptions 
        { 
            Headless = true,
            Args = new[] { "--no-sandbox", "--disable-setuid-sandbox", "--disable-dev-shm-usage" } 
        };

        // ... (Keep your Chrome path logic here) ...
        var chromePath = Environment.GetEnvironmentVariable("PUPPETEER_EXECUTABLE_PATH");
        if (!string.IsNullOrEmpty(chromePath)) launchOptions.ExecutablePath = chromePath;
        else 
        {
            var fetcher = new BrowserFetcher();
            await fetcher.DownloadAsync();
        }

        await using var browser = await Puppeteer.LaunchAsync(launchOptions);
        await using var page = await browser.NewPageAsync();

        // 1. Spoof User Agent (Critical)
        await page.SetUserAgentAsync("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

        // 2. Block heavy resources
        await page.SetRequestInterceptionAsync(true);
        page.Request += (sender, e) => 
        {
            if (e.Request.ResourceType == ResourceType.Image || 
                e.Request.ResourceType == ResourceType.Media || 
                e.Request.ResourceType == ResourceType.Font)
                e.Request.AbortAsync();
            else
                e.Request.ContinueAsync();
        };

        try 
        {
            await page.GoToAsync(SCREENER_URL, new NavigationOptions { WaitUntil = new[] { WaitUntilNavigation.DOMContentLoaded } });

            // 3. WAIT for the table data to actually exist
            // Finviz rows often have a specific hover style, or just wait 3 seconds to be safe
            await Task.Delay(3000); 

            var title = await page.GetTitleAsync();
            Console.WriteLine($"[Finviz Debug] Page Title: {title}");

            // 4. Extract Data (Generic Table Approach)
            var stocks = await page.EvaluateFunctionAsync<List<StockDataDto>>(@"() => {
                const results = [];
                // Select ALL rows in ALL tables
                const rows = document.querySelectorAll('tr');

                rows.forEach(row => {
                    const cells = row.querySelectorAll('td');
                    
                    // Finviz Overview rows usually have 11+ columns
                    // [1] is Ticker, [2] is Company, [6] is MarketCap, [8] is Price
                    if (cells.length > 10) {
                        const tickerText = cells[1].innerText.trim();
                        const priceText = cells[8].innerText.trim();
                        
                        // Heuristic: Tickers are short (<= 5 chars) and Price matches regex number
                        if (tickerText.length > 0 && tickerText.length <= 5 && !tickerText.includes('Ticker')) {
                             results.push({ 
                                Symbol: tickerText, 
                                Company: cells[2].innerText.trim(), 
                                MktCapStr: cells[6].innerText.trim(), 
                                PriceStr: priceText
                            });
                        }
                    }
                });
                return results.slice(0, 20); // Take top 20
            }");

            var candidates = new List<StockRecommendation>();
            
            foreach (var s in stocks)
            {
                if (decimal.TryParse(s.PriceStr, out var price))
                {
                    candidates.Add(new StockRecommendation
                    {
                        Symbol = s.Symbol,
                        CompanyName = s.Company,
                        Price = price,
                        MarketCap = ParseMarketCap(s.MktCapStr),
                        MatchingTheme = "Growth Screen",
                        AI_Reasoning = "Passed Finviz Growth Screen (Sales/EPS > 20%)"
                    });
                }
            }

            Console.WriteLine($"[Screener] Found {candidates.Count} stocks via Scraping.");
            return candidates;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Screener Error] {ex.Message}");
            return new List<StockRecommendation>();
        }
    }

    private decimal ParseMarketCap(string cap)
    {
        // format: "150.5B" or "400M"
        if (string.IsNullOrEmpty(cap)) return 0;
        decimal multiplier = 1;
        if (cap.EndsWith("B")) multiplier = 1_000_000_000;
        if (cap.EndsWith("M")) multiplier = 1_000_000;
        
        var numPart = cap.TrimEnd('B', 'M');
        if (decimal.TryParse(numPart, out var result))
        {
            return result * multiplier;
        }
        return 0;
    }

    private class StockDataDto 
    { 
        public string Symbol { get; set; } = "";
        public string Company { get; set; } = "";
        public string PriceStr { get; set; } = "";
        public string MktCapStr { get; set; } = "";
    }
}