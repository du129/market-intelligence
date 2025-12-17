namespace MarketIntelligence.Ingestor.Services;

public record EtfDef(string Ticker, string Name, string Category);

public class EtfService
{
    // The "Menu" for the AI
    public List<EtfDef> GetUniverse()
    {
        return new List<EtfDef>
        {
            // --- SECTORS (The SPDRs) ---
            new("XLK", "Technology Select Sector", "Tech, Software, Hardware, AI"),
            new("XLF", "Financial Select Sector", "Banks, Insurance, Berkshire"),
            new("XLV", "Health Care Select Sector", "Pharma, Biotech, Medical Devices"),
            new("XLE", "Energy Select Sector", "Oil, Gas, Exxon, Chevron"),
            new("XLC", "Communication Services", "Meta, Google, Disney, Netflix"),
            new("XLY", "Consumer Discretionary", "Amazon, Tesla, Retail, Autos"),
            new("XLP", "Consumer Staples", "Walmart, Coke, P&G"),
            new("XLI", "Industrial Select Sector", "Aerospace, Machinery, Defense"),
            new("XLB", "Materials Select Sector", "Chemicals, Mining"),
            new("XLRE", "Real Estate Select Sector", "REITs, Towers, Data Centers"),
            new("XLU", "Utilities Select Sector", "Power, Electricity, Grid"),

            // --- SPECIFIC INDUSTRIES (Thematic) ---
            new("SMH", "VanEck Semiconductor", "Chips, Nvidia, TSMC"),
            new("IGV", "iShares Tech-Software", "SaaS, Cloud"),
            new("ITA", "iShares Aerospace & Defense", "Defense, War, Boeing, Lockheed"),
            new("XBI", "SPDR Biotech", "Biotech, Drug Discovery"),
            new("KRE", "SPDR Regional Banking", "Small Banks"),
            new("ITB", "iShares US Home Construction", "Homebuilders"),
            new("METV", "Roundhill Ball Metaverse", "Metaverse, Gaming"),
            new("CIBR", "First Trust Cybersecurity", "Security, Hacking"),
            new("PAVE", "Global X Infrastructure", "Construction, Concrete, Infrastructure Bill"),
            new("TAN", "Invesco Solar", "Solar, Clean Energy"),
            new("URA", "Global X Uranium", "Nuclear Energy"),
            new("LIT", "Global X Lithium", "Batteries, EV Supply Chain"),

            // --- STYLES & SIZE ---
            new("SPY", "SPDR S&P 500", "Large Cap, Broad Market"),
            new("QQQ", "Invesco QQQ", "Nasdaq, Tech Heavy, Growth"),
            new("IWM", "iShares Russell 2000", "Small Cap, Domestic Economy"),
            new("VUG", "Vanguard Growth", "Growth Factor"),
            new("VTV", "Vanguard Value", "Value Factor"),
            new("RSP", "Invesco S&P 500 Equal Weight", "Equal Weight (Avoids concentration)"),

            // --- GLOBAL ---
            new("VEA", "Vanguard Developed Markets", "Europe, Japan, Canada"),
            new("VWO", "Vanguard Emerging Markets", "China, India, Brazil"),
            new("EEM", "iShares MSCI Emerging Markets", "Emerging Markets"),
            new("EWJ", "iShares MSCI Japan", "Japan Exposure"),
            new("MCHI", "iShares MSCI China", "China Exposure"),
            new("INDA", "iShares MSCI India", "India Exposure"),

            // --- BONDS & RATES ---
            new("TLT", "iShares 20+ Year Treasury", "Long Bonds, Rate Sensitive (Up when rates drop)"),
            new("IEF", "iShares 7-10 Year Treasury", "Medium Term Bonds"),
            new("SHY", "iShares 1-3 Year Treasury", "Cash Equivalent, Short Term"),
            new("LQD", "iShares Investment Grade Corp", "Corporate Debt"),
            new("HYG", "iShares High Yield Corp", "Junk Bonds, Risk On Credit"),

            // --- COMMODITIES ---
            new("GLD", "SPDR Gold Shares", "Gold, Inflation Hedge, Safety"),
            new("SLV", "iShares Silver Trust", "Silver"),
            new("USO", "United States Oil Fund", "Crude Oil Price"),
            new("DBC", "Invesco DB Commodity", "General Commodities (Corn, Soy, Oil, Gold)")
        };
    }

    public string GetUniverseAsString()
    {
        var sb = new System.Text.StringBuilder();
        foreach (var etf in GetUniverse())
        {
            sb.AppendLine($"- {etf.Ticker}: {etf.Name} ({etf.Category})");
        }
        return sb.ToString();
    }
}