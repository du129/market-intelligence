namespace MarketIntelligence.Shared;

public class ChartPoint
{
    public string Label { get; set; } = string.Empty; // e.g. "2025-11-25"
    public decimal Value1 { get; set; } // e.g. MSFT Price
    public decimal Value2 { get; set; } // e.g. TSLA Price
}

public class ChartResponse
{
    public string Title { get; set; } = string.Empty;
    public string Series1Name { get; set; } = string.Empty;
    public string Series2Name { get; set; } = string.Empty;
    public List<ChartPoint> Data { get; set; } = new();
}