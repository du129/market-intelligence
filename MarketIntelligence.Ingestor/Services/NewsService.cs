// --- ADD THESE LINES TO THE VERY TOP ---
#pragma warning disable SKEXP0001 
#pragma warning disable SKEXP0010
#pragma warning disable SKEXP0020
#pragma warning disable SKEXP0050
// ---------------------------------------

using MarketIntelligence.Ingestor; 
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel.Memory;
using NewsAPI;
using NewsAPI.Constants;
using NewsAPI.Models;

namespace MarketIntelligence.Ingestor.Services;

public class NewsService
{
    private readonly NewsApiClient _client;
    private readonly ISemanticTextMemory _memory;

    public NewsService(IConfiguration config, ISemanticTextMemory memory)
    {
        _client = new NewsApiClient(config["NewsApiKey"]);
        _memory = memory;
    }

    public async Task IngestAsync(string symbol)
    {
        Console.WriteLine($"[NewsAPI] Searching headlines for {symbol}...");

        try 
        {
            var response = _client.GetEverything(new EverythingRequest
            {
                Q = symbol,
                SortBy = SortBys.PublishedAt, // Get the absolute latest
                Language = Languages.EN,
                From = DateTime.Now.AddDays(-3)
            });

            if (response.Status == Statuses.Ok)
            {
                // Take top 5 articles
                foreach (var article in response.Articles.Take(5)) 
                {
                    if (string.IsNullOrEmpty(article.Title)) continue;

                    Console.WriteLine($"   -> Found: {article.Title.Substring(0, Math.Min(30, article.Title.Length))}...");

                    // SAVE TO VECTOR DATABASE (Azure AI Search)
                    var id = Guid.NewGuid().ToString();
                    var text = $"{article.Title} - {article.Description}";
                    var desc = $"Published: {article.PublishedAt} | Source: {article.Source.Name}";

                    await _memory.SaveInformationAsync(
                        collection: "market-news",
                        id: id,
                        text: text,
                        description: desc
                    );
                }
                Console.WriteLine($"[News] Indexed {response.Articles.Count} articles for {symbol}.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[News Error] {ex.Message}");
        }
    }
}