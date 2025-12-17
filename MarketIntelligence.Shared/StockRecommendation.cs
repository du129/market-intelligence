using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MarketIntelligence.Shared;

[Table("StockRecommendations")]
public class StockRecommendation
{
    [Key]
    public int Id { get; set; }
    public DateTime DateGenerated { get; set; } = DateTime.UtcNow;
    
    public string Symbol { get; set; } = string.Empty; // e.g. NVDA
    public string CompanyName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    
    // The "Quant" Reasons
    public decimal MarketCap { get; set; }
    public decimal SMA200 { get; set; }
    public decimal ROE { get; set; }
    
    // The "Macro" Reason
    public string MatchingTheme { get; set; } = string.Empty; // e.g. "AI Buildout"
    public string AI_Reasoning { get; set; } = string.Empty; // Why AI likes it
}