using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MarketIntelligence.Shared;

[Table("EtfRecommendations")]
public class EtfRecommendation
{
    [Key]
    public int Id { get; set; }
    public DateTime DateGenerated { get; set; } = DateTime.UtcNow;

    [MaxLength(10)]
    public string Ticker { get; set; } = string.Empty; // e.g. "XLK"
    
    public string FundName { get; set; } = string.Empty; // e.g. "Technology Select Sector SPDR"
    
    public string MatchedTheme { get; set; } = string.Empty; // e.g. "AI Buildout"
    public string AI_Reasoning { get; set; } = string.Empty; // Why this ETF fits the theme
}