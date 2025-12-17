using Google.Apis.CustomSearchAPI.v1;
using Google.Apis.Services;
using Microsoft.Extensions.Configuration;
using PuppeteerSharp;

namespace MarketIntelligence.Ingestor.Services;

public class MacroScoutService
{
    private readonly CustomSearchAPIService _googleService;
    private readonly string _cx;

    public MacroScoutService(IConfiguration config)
    {
        _googleService = new CustomSearchAPIService(new BaseClientService.Initializer
        {
            ApiKey = config["Google:ApiKey"]
        });
        _cx = config["Google:SearchEngineId"]!;
    }

    public async Task<string> FindAndScrapeOutlookAsync(string bankName, int year)
    {
        var query = $"{bankName} investment outlook {year} market trends";
        Console.WriteLine($"[Scout] Googling: '{query}'...");

        var request = _googleService.Cse.List();
        request.Cx = _cx;
        request.Q = query;

        try 
        {
            var result = await request.ExecuteAsync();

            if (result.Items?.Count > 0)
            {
                // Take the first result that isn't a PDF (easier to scrape text from HTML for now)
                var topHit = result.Items.FirstOrDefault(); 
                
                if (topHit != null)
                {
                    Console.WriteLine($"[Scout] Found: {topHit.Title}");
                    Console.WriteLine($"[Scout] URL: {topHit.Link}");
                    return await ScrapeUrlAsync(topHit.Link);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Scout] Google Error: {ex.Message}");
        }

        return string.Empty;
    }

    private async Task<string> ScrapeUrlAsync(string url)
    {
        Console.WriteLine("[Scout] Launching Headless Browser...");

        var launchOptions = new LaunchOptions 
        { 
            Headless = true,
            Args = new[] { 
                "--no-sandbox", 
                "--disable-setuid-sandbox",
                "--disable-http2" // FIX: Often solves the HTTP2 protocol error
            } 
        };

        var chromePath = Environment.GetEnvironmentVariable("PUPPETEER_EXECUTABLE_PATH");

        if (!string.IsNullOrEmpty(chromePath))
        {
            // DOCKER MODE: Use the installed ARM64 Chromium
            Console.WriteLine($"[Scout] Using pre-installed Chrome at: {chromePath}");
            launchOptions.ExecutablePath = chromePath;
        }
        else
        {
            // LOCAL MAC MODE: Download a local revision
            // FIX 1: Removed 'using' keyword here
            Console.WriteLine("[Scout] Downloading local Chrome revision...");
            var browserFetcher = new BrowserFetcher(); 
            await browserFetcher.DownloadAsync();
        }

        await using var browser = await Puppeteer.LaunchAsync(launchOptions);
        await using var page = await browser.NewPageAsync();
        
        await page.SetUserAgentAsync("Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

        // Block heavy resources
        await page.SetRequestInterceptionAsync(true);
        page.Request += (sender, e) => 
        {
            if (e.Request.ResourceType == ResourceType.Image || 
                e.Request.ResourceType == ResourceType.Font || 
                e.Request.ResourceType == ResourceType.Media)
                e.Request.AbortAsync();
            else
                e.Request.ContinueAsync();
        };

        try 
        {
            // FIX 2: Fixed casing to DOMContentLoaded
            await page.GoToAsync(url, new NavigationOptions 
            { 
                WaitUntil = new[] { WaitUntilNavigation.DOMContentLoaded }, 
                Timeout = 30000 
            });
            
            var content = await page.EvaluateFunctionAsync<string[]>(
                "() => Array.from(document.querySelectorAll('p')).map(p => p.innerText)"
            );
            
            return string.Join("\n", content);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Scout Error] Could not scrape {url}: {ex.Message}");
            return "";
        }
    }
}