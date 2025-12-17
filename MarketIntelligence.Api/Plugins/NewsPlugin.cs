#pragma warning disable SKEXP0001, SKEXP0010, SKEXP0020, SKEXP0050

using System.ComponentModel;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Memory;

namespace MarketIntelligence.Api.Plugins;

public class NewsPlugin
{
    private readonly ISemanticTextMemory _memory;

    public NewsPlugin(ISemanticTextMemory memory)
    {
        _memory = memory;
    }

    [KernelFunction, Description("Searches for financial news.")]
    public async Task<string> SearchNews([Description("Topic (e.g. 'Microsoft AI')")] string query)
    {
        Console.WriteLine($"[System] Vector Search for '{query}'...");
        
        var results = _memory.SearchAsync("market-news", query, limit: 3, minRelevanceScore: 0.5);
        var output = new System.Text.StringBuilder();

        await foreach (var result in results)
        {
            output.AppendLine($"[Article] {result.Metadata.Description}");
            output.AppendLine($"Summary: {result.Metadata.Text}\n");
        }

        return output.Length == 0 ? "No relevant news found." : output.ToString();
    }
}