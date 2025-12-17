using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MarketIntelligence.Shared;

[Table("MarketThemes")]
public class MarketTheme
{
    [Key]
    public int Id { get; set; }

    public DateTime RecordedDate { get; set; } = DateTime.UtcNow;

    [MaxLength(100)]
    public string Source { get; set; } = string.Empty; // e.g., "Morgan Stanley"

    [MaxLength(200)]
    public string ThemeTitle { get; set; } = string.Empty; // e.g., "AI Infrastructure Boom"

    public string Sentiment { get; set; } = string.Empty; // "Bullish", "Bearish", "Neutral"

    public string Reasoning { get; set; } = string.Empty; // The AI's summary of why
}