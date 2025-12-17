using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MarketIntelligence.Shared;

[Table("Stocks")]
public class StockData
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(10)]
    public string Symbol { get; set; } = string.Empty;
    
    public DateTime Date { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal Close { get; set; }
    
    public long Volume { get; set; }
}