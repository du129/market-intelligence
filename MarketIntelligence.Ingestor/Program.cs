#pragma warning disable SKEXP0001, SKEXP0010, SKEXP0020, SKEXP0050, SKEXP0052

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureAISearch;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.Plugins.Memory;
using System.Text.Json;
using MarketIntelligence.Shared;
using MarketIntelligence.Ingestor;
using MarketIntelligence.Ingestor.Services;

class Program
{
    static async Task Main(string[] args)
    {
        // 1. Load Configuration
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        Console.WriteLine("--- Starting Alpha Hunter (ETF Edition) ---");

        // 2. Ensure Database Schema is Up-to-Date
        using (var db = new MarketContext())
        {
            Console.WriteLine("[Init] Applying Database Migrations...");
            await db.Database.MigrateAsync();
        }

        // 3. Initialize Services
        var kernel = BuildKernel(config);
        var memory = BuildMemory(config); 
        var scoutService = new MacroScoutService(config);
        var stockService = new StockService(config);
        var newsService = new NewsService(config, memory);
        var etfService = new EtfService();

        // =========================================================
        // PART A: MACRO ANALYSIS (Scrape Banks for Insights)
        // =========================================================
        var banks = new[] { "Morgan Stanley", "J.P. Morgan", "BlackRock", "Goldman Sachs" };
        
        foreach (var bank in banks)
        {
            await RunMacroAnalysis(bank, scoutService, kernel);
        }

        // =========================================================
        // PART B: THE ETF MATCHMAKER (Connect Themes to Funds)
        // =========================================================
        Console.WriteLine("\n[Matchmaker] Retrieving recent Macro Themes from DB...");
        
        using var dbContext = new MarketContext();
        
        // 1. Get recent "Bullish" themes (last 24 hours)
        var recentThemes = await dbContext.MarketThemes
            .Where(t => t.Sentiment == "Bullish" && t.RecordedDate > DateTime.UtcNow.AddHours(-24))
            .ToListAsync();

        if (!recentThemes.Any())
        {
            Console.WriteLine("[Matchmaker] No new bullish themes found today. Skipping ETF selection.");
        }
        else
        {
            Console.WriteLine($"[Matchmaker] Found {recentThemes.Count} active themes. Analyzing ETF fit...");
            
            var etfMenu = etfService.GetUniverseAsString();
            var chat = kernel.GetRequiredService<IChatCompletionService>();

            foreach (var theme in recentThemes)
            {
                Console.WriteLine($"\n   >>> Matching Theme: '{theme.ThemeTitle}' ({theme.Source})");

                // 2. The Agent Prompt
                var prompt = $@"
                    I am an Investment Strategist Agent.
                    
                    CURRENT MARKET THEME:
                    Title: '{theme.ThemeTitle}'
                    Context: {theme.Reasoning}
                    Source: {theme.Source}

                    AVAILABLE ETF UNIVERSE:
                    {etfMenu}

                    TASK:
                    Select the top 1 or 2 ETFs from the universe that DIRECTLY benefit from this theme.
                    Example: If theme is 'High Interest Rates', select 'TLT'.
                    Example: If theme is 'AI Hardware', select 'SMH'.
                    
                    If no ETF in the list is a strong match, return an empty array [].

                    RETURN JSON ONLY:
                    [ {{ ""ticker"": ""XLK"", ""reason"": ""Direct exposure to tech..."" }} ]
                ";

                var history = new ChatHistory("You are a Portfolio Manager.");
                history.AddUserMessage(prompt);

                try 
                {
                    // 3. AI Selection
                    var result = await chat.GetChatMessageContentAsync(history);
                    var jsonStr = CleanJson(result.Content!);
                    
                    var matches = JsonSerializer.Deserialize<List<EtfMatchDto>>(jsonStr);

                    if (matches != null && matches.Any())
                    {
                        foreach (var match in matches)
                        {
                            // --- VALIDATION: Ensure Ticker exists ---
                            var etfInfo = etfService.GetUniverse()
                                .FirstOrDefault(e => e.Ticker.Equals(match.ticker, StringComparison.OrdinalIgnoreCase));

                            if (etfInfo == null)
                            {
                                Console.WriteLine($"      [Skip] AI suggested '{match.ticker}' which is not in our known ETF Universe.");
                                continue;
                            }

                            Console.WriteLine($"      -> Selected: {etfInfo.Ticker} | Reason: {match.reason}");

                            // --- RATE LIMITING ---
                            // Alpha Vantage Limit: 5 calls/min. Wait 15s.
                            await Task.Delay(15000); 

                            // --- DATA GAP FIX: Fetch & SAVE History ---
                            decimal currentPrice = 0;
                            try {
                                var priceHistory = await stockService.GetHistoryAsync(etfInfo.Ticker);
                                
                                if (priceHistory.Any())
                                {
                                    currentPrice = priceHistory.FirstOrDefault()?.Close ?? 0;

                                    // SAVE HISTORY TO STOCKS TABLE (So Charts work!)
                                    foreach (var h in priceHistory)
                                    {
                                        if (!dbContext.Stocks.Any(s => s.Symbol == h.Symbol && s.Date == h.Date))
                                        {
                                            dbContext.Stocks.Add(h);
                                        }
                                    }
                                    await dbContext.SaveChangesAsync();
                                    Console.WriteLine($"      [DB] Saved price history for {etfInfo.Ticker}");
                                }
                            } catch { /* Ignore fetch errors */ }

                            // 5. Save Recommendation (Avoid Duplicates)
                            bool alreadyRecommended = dbContext.Recommendations.Any(r => 
                                r.Symbol == etfInfo.Ticker && 
                                r.MatchingTheme == theme.ThemeTitle &&
                                r.DateGenerated > DateTime.UtcNow.AddHours(-48));

                            if (!alreadyRecommended)
                            {
                                dbContext.Recommendations.Add(new StockRecommendation
                                {
                                    Symbol = etfInfo.Ticker,
                                    CompanyName = etfInfo.Name,
                                    Price = currentPrice,
                                    MarketCap = 0,
                                    MatchingTheme = theme.ThemeTitle,
                                    AI_Reasoning = $"{theme.Source} says: {match.reason}"
                                });
                                Console.WriteLine($"      [DB] Recommendation saved.");
                            }
                            else
                            {
                                Console.WriteLine($"      [DB] Already recommended recently. Skipping.");
                            }
                        }
                        await dbContext.SaveChangesAsync();
                    }
                    else
                    {
                        Console.WriteLine("      (No strong ETF match found for this theme)");
                    }
                }
                catch (Exception ex) { Console.WriteLine($"[Matchmaker Error] {ex.Message}"); }
            }
        }

        // =========================================================
        // PART C: STANDARD STOCK & NEWS INGESTION
        // =========================================================
        // Added SPY to this list so you can ask about the general market
        var symbols = new[] { "MSFT", "TSLA", "NVDA", "AAPL", "SPY" }; 
        
        Console.WriteLine("\n[Pipeline] Updating standard watch list...");
        
        foreach (var symbol in symbols)
        {
            try 
            {
                // A. Real Stock Data
                await Task.Delay(15000); // Rate Limit Protection
                var history = await stockService.GetHistoryAsync(symbol);
                
                if (history.Any())
                {
                    using var db = new MarketContext();
                    foreach (var item in history)
                    {
                        if (!db.Stocks.Any(s => s.Symbol == item.Symbol && s.Date == item.Date))
                        {
                            db.Stocks.Add(item);
                            Console.WriteLine($"[SQL] Inserted {item.Symbol}: {item.Close:C}");
                        }
                    }
                    await db.SaveChangesAsync();
                }

                // B. Real News Data
                await newsService.IngestAsync(symbol);
            }
            catch (Exception ex) { Console.WriteLine($"[Pipeline Error] {symbol}: {ex.Message}"); }
        }

        Console.WriteLine("\nPipeline Run Complete.");
    }

    // =========================================================
    // HELPER METHODS
    // =========================================================

    private static async Task RunMacroAnalysis(string bank, MacroScoutService scout, Kernel kernel)
    {
        Console.WriteLine($"\n[Macro] Analyzing {bank}...");
        
        var text = await scout.FindAndScrapeOutlookAsync(bank, 2025);
        if (string.IsNullOrWhiteSpace(text)) return;

        var cleanText = text.Substring(0, Math.Min(text.Length, 12000));

        var chat = kernel.GetRequiredService<IChatCompletionService>();
        var history = new ChatHistory("You are a Financial Analyst.");
        history.AddUserMessage($@"
            Extract top 3 investment themes from this text.
            Return ONLY a JSON array: [{{ ""title"": ""..."", ""sentiment"": ""Bullish"", ""reason"": ""..."" }}]
            If text is garbage, return [].
            TEXT: {cleanText}");

        try 
        {
            var result = await chat.GetChatMessageContentAsync(history);
            var json = CleanJson(result.Content!);
            var themes = JsonSerializer.Deserialize<List<ThemeDto>>(json);

            if (themes != null)
            {
                using var db = new MarketContext();
                foreach (var t in themes)
                {
                    if (!db.MarketThemes.Any(x => x.Source == bank && x.ThemeTitle == t.title && x.RecordedDate > DateTime.UtcNow.AddHours(-24)))
                    {
                        db.MarketThemes.Add(new MarketTheme 
                        { 
                            Source = bank, 
                            ThemeTitle = t.title, 
                            Sentiment = t.sentiment, 
                            Reasoning = t.reason 
                        });
                        Console.WriteLine($"[SQL] Saved Theme: {t.title} ({t.sentiment})");
                    }
                }
                await db.SaveChangesAsync();
            }
        }
        catch (Exception ex) { Console.WriteLine($"[Macro Error] {ex.Message}"); }
    }

    private static string CleanJson(string aiResponse)
    {
        var clean = aiResponse.Replace("```json", "").Replace("```", "").Trim();
        int startIndex = -1;
        int endIndex = -1;

        int arrayStart = clean.IndexOf('[');
        int objectStart = clean.IndexOf('{');

        if (arrayStart >= 0 && (objectStart == -1 || arrayStart < objectStart))
        {
            startIndex = arrayStart;
            endIndex = clean.LastIndexOf(']');
        }
        else if (objectStart >= 0)
        {
            startIndex = objectStart;
            endIndex = clean.LastIndexOf('}');
        }

        if (startIndex >= 0 && endIndex > startIndex)
        {
            clean = clean.Substring(startIndex, endIndex - startIndex + 1);
        }
        return clean;
    }

    private static Kernel BuildKernel(IConfiguration config)
    {
        var builder = Kernel.CreateBuilder();
        builder.AddAzureOpenAIChatCompletion(
            config["AI:DeploymentChat"]!, 
            config["AI:Endpoint"]!,
            config["AI:Key"]!
        );
        return builder.Build();
    }

    private static ISemanticTextMemory BuildMemory(IConfiguration config)
    {
        var embeddingService = new AzureOpenAITextEmbeddingGenerationService(
            config["AI:DeploymentEmbedding"]!,
            config["AI:Endpoint"]!,
            config["AI:Key"]!
        );
        var memoryStore = new AzureAISearchMemoryStore(
            config["Search:Endpoint"]!,
            config["Search:Key"]!
        );
        return new MemoryBuilder()
            .WithTextEmbeddingGeneration(embeddingService)
            .WithMemoryStore(memoryStore)
            .Build();
    }

    // --- DTOs ---
    private class ThemeDto 
    { 
        public string title { get; set; } = "";
        public string sentiment { get; set; } = "";
        public string reason { get; set; } = "";
    }

    private class EtfMatchDto
    {
        public string ticker { get; set; } = "";
        public string reason { get; set; } = "";
    }
}